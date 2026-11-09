using System.Buffers.Binary;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Drawing = DocumentFormat.OpenXml.Drawing;
using Picture = DocumentFormat.OpenXml.Drawing.Pictures;
using WordDrawing = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace Aprint.DocumentSession;

public sealed class DocxSessionWriter : IDocumentSessionWriter
{
    private const long EmusPerTwip = 635;
    private const long EmusPerPixelAt96Dpi = 9525;
    private readonly string _workingPath;
    private readonly string _outputPath;
    private int _imageCount;
    private bool _completed;
    private bool _disposed;

    private DocxSessionWriter(string workingPath, string outputPath)
    {
        _workingPath = workingPath;
        _outputPath = outputPath;
    }

    public int ImageCount => _imageCount;

    public static DocxSessionWriter CreateBlank(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (!string.Equals(Path.GetExtension(outputPath), ".docx", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The output must be a .docx file.", nameof(outputPath));
        }

        var outputFullPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(outputFullPath)!;
        Directory.CreateDirectory(outputDirectory);
        var workingPath = CreateWorkingPath(outputFullPath);
        try
        {
            using (var document = WordprocessingDocument.Create(workingPath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body(new Paragraph()));
                mainPart.Document.Save();
            }

            return new DocxSessionWriter(workingPath, outputFullPath);
        }
        catch
        {
            TryDelete(workingPath);
            throw;
        }
    }

    public static DocxSessionWriter Create(string templatePath, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (!string.Equals(Path.GetExtension(templatePath), ".docx", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The template must be a .docx file.", nameof(templatePath));
        }

        if (!string.Equals(Path.GetExtension(outputPath), ".docx", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The output must be a .docx file.", nameof(outputPath));
        }

        var templateFullPath = Path.GetFullPath(templatePath);
        var outputFullPath = Path.GetFullPath(outputPath);
        if (string.Equals(templateFullPath, outputFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The output cannot replace the selected template.", nameof(outputPath));
        }

        if (!File.Exists(templateFullPath))
        {
            throw new FileNotFoundException("The selected document template was not found.", templateFullPath);
        }

        var outputDirectory = Path.GetDirectoryName(outputFullPath)!;
        Directory.CreateDirectory(outputDirectory);
        var workingPath = CreateWorkingPath(outputFullPath);

        try
        {
            File.Copy(templateFullPath, workingPath);
            using var document = WordprocessingDocument.Open(workingPath, true);
            var mainPart = document.MainDocumentPart ?? document.AddMainDocumentPart();
            mainPart.Document ??= new Document();
            var body = mainPart.Document.Body;
            if (body is null)
            {
                body = new Body();
                mainPart.Document.Append(body);
            }

            if (body.ChildElements.All(element => element is SectionProperties))
            {
                var paragraph = new Paragraph();
                var sectionProperties = body.GetFirstChild<SectionProperties>();
                if (sectionProperties is null)
                {
                    body.Append(paragraph);
                }
                else
                {
                    body.InsertBefore(paragraph, sectionProperties);
                }
            }

            mainPart.Document.Save();

            return new DocxSessionWriter(workingPath, outputFullPath);
        }
        catch
        {
            TryDelete(workingPath);
            throw;
        }
    }

    public Task AddImageAsync(ReadOnlyMemory<byte> pngBytes, CancellationToken cancellationToken = default)
    {
        EnsureActive();
        var image = pngBytes.ToArray();
        return Task.Run(() => AddPngImage(image), cancellationToken);
    }

    public string? Complete()
    {
        EnsureActive();
        if (_imageCount == 0)
        {
            File.Delete(_workingPath);
            _completed = true;
            return null;
        }

        if (File.Exists(_outputPath))
        {
            File.Replace(_workingPath, _outputPath, null);
        }
        else
        {
            File.Move(_workingPath, _outputPath);
        }

        _completed = true;
        return _outputPath;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_completed)
        {
            TryDelete(_workingPath);
        }
    }

    private void AddPngImage(byte[] pngBytes)
    {
        EnsureActive();
        var (pixelWidth, pixelHeight) = ReadPngDimensions(pngBytes);
        using var document = WordprocessingDocument.Open(_workingPath, true);
        var mainPart = document.MainDocumentPart
            ?? throw new InvalidDataException("The template does not contain a main document part.");
        var body = mainPart.Document.Body
            ?? throw new InvalidDataException("The template does not contain a main document body.");
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var imageStream = new MemoryStream(pngBytes, writable: false))
        {
            imagePart.FeedData(imageStream);
        }

        var relationshipId = mainPart.GetIdOfPart(imagePart);
        var (width, height) = GetImageExtent(body, pixelWidth, pixelHeight);
        var imageNumber = checked((uint)_imageCount + 1);
        var imageName = $"Aprint capture {imageNumber}";
        var paragraph = new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(CreatePicture(relationshipId, imageName, imageNumber, width, height)));

        var sectionProperties = body.GetFirstChild<SectionProperties>();
        if (_imageCount > 0)
        {
            var pageBreak = new Paragraph(new Run(new Break { Type = BreakValues.Page }));
            if (sectionProperties is null)
            {
                body.Append(pageBreak, paragraph);
            }
            else
            {
                body.InsertBefore(pageBreak, sectionProperties);
                body.InsertBefore(paragraph, sectionProperties);
            }
        }
        else if (sectionProperties is null)
        {
            body.Append(paragraph);
        }
        else
        {
            body.InsertBefore(paragraph, sectionProperties);
        }

        mainPart.Document.Save();
        _imageCount++;
    }

    private static DocumentFormat.OpenXml.Wordprocessing.Drawing CreatePicture(
        string relationshipId,
        string name,
        uint imageNumber,
        long width,
        long height)
    {
        var picture = new Picture.Picture(
            new Picture.NonVisualPictureProperties(
                new Picture.NonVisualDrawingProperties { Id = imageNumber, Name = name },
                new Picture.NonVisualPictureDrawingProperties()),
            new Picture.BlipFill(
                new Drawing.Blip { Embed = relationshipId },
                new Drawing.Stretch(new Drawing.FillRectangle())),
            new Picture.ShapeProperties(
                new Drawing.Transform2D(
                    new Drawing.Offset { X = 0L, Y = 0L },
                    new Drawing.Extents { Cx = width, Cy = height }),
                new Drawing.PresetGeometry(new Drawing.AdjustValueList())
                {
                    Preset = Drawing.ShapeTypeValues.Rectangle,
                }));
        var graphic = new Drawing.Graphic(
            new Drawing.GraphicData(picture)
            {
                Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture",
            });
        var inline = new WordDrawing.Inline(
            new WordDrawing.Extent { Cx = width, Cy = height },
            new WordDrawing.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
            new WordDrawing.DocProperties { Id = imageNumber, Name = name },
            new WordDrawing.NonVisualGraphicFrameDrawingProperties(
                new Drawing.GraphicFrameLocks { NoChangeAspect = true }),
            graphic)
        {
            DistanceFromTop = 0U,
            DistanceFromBottom = 0U,
            DistanceFromLeft = 0U,
            DistanceFromRight = 0U,
        };

        return new DocumentFormat.OpenXml.Wordprocessing.Drawing(inline);
    }

    private static (long Width, long Height) GetImageExtent(Body body, uint pixelWidth, uint pixelHeight)
    {
        var section = body.GetFirstChild<SectionProperties>();
        var pageSize = section?.GetFirstChild<PageSize>();
        var margins = section?.GetFirstChild<PageMargin>();
        var pageWidth = pageSize?.Width?.Value ?? 12240U;
        var pageHeight = pageSize?.Height?.Value ?? 15840U;
        var marginLeft = Math.Max(0, margins?.Left?.Value ?? 1440);
        var marginRight = Math.Max(0, margins?.Right?.Value ?? 1440);
        var marginTop = Math.Max(0, margins?.Top?.Value ?? 1440);
        var marginBottom = Math.Max(0, margins?.Bottom?.Value ?? 1440);
        var maxWidth = Math.Max(1L, (long)pageWidth - marginLeft - marginRight) * EmusPerTwip;
        var maxHeight = Math.Max(1L, (long)pageHeight - marginTop - marginBottom) * EmusPerTwip;
        var imageWidth = pixelWidth * EmusPerPixelAt96Dpi;
        var imageHeight = pixelHeight * EmusPerPixelAt96Dpi;
        var scale = Math.Min(1d, Math.Min((double)maxWidth / imageWidth, (double)maxHeight / imageHeight));
        return ((long)(imageWidth * scale), (long)(imageHeight * scale));
    }

    private static string CreateWorkingPath(string outputFullPath) => Path.Combine(
        Path.GetDirectoryName(outputFullPath)!,
        $".{Path.GetFileNameWithoutExtension(outputFullPath)}.{Guid.NewGuid():N}.session.docx");

    private static (uint Width, uint Height) ReadPngDimensions(byte[] pngBytes)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (pngBytes.Length < 24 || !pngBytes.AsSpan(0, 8).SequenceEqual(signature))
        {
            throw new InvalidDataException("The captured image is not a valid PNG stream.");
        }

        var width = BinaryPrimitives.ReadUInt32BigEndian(pngBytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(pngBytes.AsSpan(20, 4));
        if (width == 0 || height == 0)
        {
            throw new InvalidDataException("The PNG image dimensions are invalid.");
        }

        return (width, height);
    }

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
        {
            throw new InvalidOperationException("The document session is already complete.");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}