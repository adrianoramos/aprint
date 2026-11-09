using Aprint.DocumentSession;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Aprint.Tests.Application;

public sealed class DocxSessionWriterTests
{
    [Fact]
    public async Task AddsEachImageToTemplateAndPublishesOnlyWhenCompleted()
    {
        var directory = CreateTestDirectory();
        var templatePath = Path.Combine(directory, "template.docx");
        var outputPath = Path.Combine(directory, "report.docx");
        CreateTemplate(templatePath);

        using var writer = DocxSessionWriter.Create(templatePath, outputPath);

        Assert.False(File.Exists(outputPath));
        await writer.AddImageAsync(CreatePngBytes());
        await writer.AddImageAsync(CreatePngBytes());
        Assert.Equal(2, writer.ImageCount);
        Assert.False(File.Exists(outputPath));

        Assert.Equal(outputPath, writer.Complete());

        using var document = WordprocessingDocument.Open(outputPath, false);
        var body = document.MainDocumentPart!.Document.Body!;
        Assert.Contains("Template content", body.InnerText, StringComparison.Ordinal);
        Assert.Equal(2, body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().Count());
        Assert.Single(body.Descendants<Break>(), item => item.Type?.Value == BreakValues.Page);
        Assert.Equal(2, document.MainDocumentPart.ImageParts.Count());
        Assert.Empty(new OpenXmlValidator().Validate(document));
    }

    [Fact]
    public async Task StartsSessionFromBlankTemplateWithoutDocumentBody()
    {
        var directory = CreateTestDirectory();
        var templatePath = Path.Combine(directory, "blank.docx");
        var outputPath = Path.Combine(directory, "report.docx");
        using (var document = WordprocessingDocument.Create(templatePath, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            mainPart.Document.Save();
        }

        using var writer = DocxSessionWriter.Create(templatePath, outputPath);
        await writer.AddImageAsync(CreatePngBytes());

        Assert.Equal(outputPath, writer.Complete());
        using var result = WordprocessingDocument.Open(outputPath, false);
        Assert.Single(result.MainDocumentPart!.Document.Body!.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>());
        Assert.Empty(new OpenXmlValidator().Validate(result));
    }

    [Fact]
    public async Task CreatesBlankDocumentSessionAndAddsCapture()
    {
        var directory = CreateTestDirectory();
        var outputPath = Path.Combine(directory, "blank-report.docx");

        using var writer = DocxSessionWriter.CreateBlank(outputPath);
        await writer.AddImageAsync(CreatePngBytes());

        Assert.Equal(outputPath, writer.Complete());
        using var document = WordprocessingDocument.Open(outputPath, false);
        Assert.Single(document.MainDocumentPart!.Document.Body!.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>());
        Assert.Empty(new OpenXmlValidator().Validate(document));
    }

    [Fact]
    public void CompletingWithoutImagesDoesNotCreateOutput()
    {
        var directory = CreateTestDirectory();
        var templatePath = Path.Combine(directory, "template.docx");
        var outputPath = Path.Combine(directory, "report.docx");
        CreateTemplate(templatePath);

        using var writer = DocxSessionWriter.Create(templatePath, outputPath);

        Assert.Null(writer.Complete());
        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void RejectsOutputPathThatWouldOverwriteTemplate()
    {
        var directory = CreateTestDirectory();
        var templatePath = Path.Combine(directory, "template.docx");
        CreateTemplate(templatePath);

        Assert.Throws<ArgumentException>(() => DocxSessionWriter.Create(templatePath, templatePath));
    }

    private static string CreateTestDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Aprint.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void CreateTemplate(string path)
    {
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text("Template content")))));
        mainPart.Document.Save();
    }

    private static byte[] CreatePngBytes()
    {
        using var bitmap = new Bitmap(8, 6);
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }
}