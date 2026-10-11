using System.Buffers.Binary;
using System.Drawing.Imaging;
using System.Text;

namespace Aprint.Infrastructure.Images;

public static class ImageMetadataWriter
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly byte[] SoftwareText = Encoding.ASCII.GetBytes("Software\0Aprint");
    private static readonly byte[] ExifIdentifier = Encoding.ASCII.GetBytes("Exif\0\0");

    public static void SaveWithSoftwareMetadata(Image image, Stream destination, ImageFormat format)
    {
        using var encodedImage = new MemoryStream();
        image.Save(encodedImage, format);
        var bytes = encodedImage.ToArray();

        var output = format.Guid == ImageFormat.Png.Guid
            ? AddPngSoftwareText(bytes)
            : format.Guid == ImageFormat.Jpeg.Guid
                ? AddJpegSoftwareExif(bytes)
                : bytes;

        destination.Write(output);
    }

    private static byte[] AddPngSoftwareText(byte[] image)
    {
        if (image.Length < PngSignature.Length + 8 || !image.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
        {
            throw new InvalidDataException("The encoded image is not a valid PNG.");
        }

        var headerOffset = PngSignature.Length;
        var headerLength = BinaryPrimitives.ReadUInt32BigEndian(image.AsSpan(headerOffset, sizeof(uint)));
        if (!image.AsSpan(headerOffset + sizeof(uint), 4).SequenceEqual("IHDR"u8) ||
            headerLength > image.Length - headerOffset - 12)
        {
            throw new InvalidDataException("The encoded PNG has an invalid header.");
        }

        var insertionOffset = checked(headerOffset + sizeof(uint) + 4 + (int)headerLength + sizeof(uint));
        var exifChunk = CreatePngChunk("eXIf"u8, CreateExifTiffPayload());
        var textChunk = CreatePngChunk("tEXt"u8, SoftwareText);
        var result = new byte[image.Length + exifChunk.Length + textChunk.Length];
        image.AsSpan(0, insertionOffset).CopyTo(result);
        exifChunk.CopyTo(result, insertionOffset);
        textChunk.CopyTo(result, insertionOffset + exifChunk.Length);
        image.AsSpan(insertionOffset).CopyTo(result.AsSpan(insertionOffset + exifChunk.Length + textChunk.Length));
        return result;
    }

    private static byte[] CreatePngChunk(ReadOnlySpan<byte> chunkType, ReadOnlySpan<byte> data)
    {
        var chunk = new byte[sizeof(uint) + 4 + data.Length + sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(chunk, (uint)data.Length);
        chunkType.CopyTo(chunk.AsSpan(sizeof(uint), 4));
        data.CopyTo(chunk.AsSpan(sizeof(uint) + 4));
        BinaryPrimitives.WriteUInt32BigEndian(
            chunk.AsSpan(chunk.Length - sizeof(uint)),
            CalculateCrc32(chunk.AsSpan(sizeof(uint), 4 + data.Length)));
        return chunk;
    }

    private static uint CalculateCrc32(ReadOnlySpan<byte> data)
    {
        var crc = uint.MaxValue;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320 : crc >> 1;
            }
        }

        return ~crc;
    }

    private static byte[] AddJpegSoftwareExif(byte[] image)
    {
        if (image.Length < 2 || image[0] != 0xff || image[1] != 0xd8)
        {
            throw new InvalidDataException("The encoded image is not a valid JPEG.");
        }

        var insertionOffset = FindJpegApp0End(image);
        var segment = CreateJpegExifSegment();
        var result = new byte[image.Length + segment.Length];
        image.AsSpan(0, insertionOffset).CopyTo(result);
        segment.CopyTo(result, insertionOffset);
        image.AsSpan(insertionOffset).CopyTo(result.AsSpan(insertionOffset + segment.Length));
        return result;
    }

    private static int FindJpegApp0End(byte[] image)
    {
        var offset = 2;
        while (offset + 4 <= image.Length && image[offset] == 0xff && image[offset + 1] == 0xe0)
        {
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(image.AsSpan(offset + 2, sizeof(ushort)));
            if (segmentLength < 2 || segmentLength > image.Length - offset - 2)
            {
                throw new InvalidDataException("The encoded JPEG has an invalid APP0 segment.");
            }

            offset += 2 + segmentLength;
        }

        return offset;
    }

    private static byte[] CreateJpegExifSegment()
    {
        var tiffPayload = CreateExifTiffPayload();
        var exifPayloadLength = ExifIdentifier.Length + tiffPayload.Length;
        var segment = new byte[2 + sizeof(ushort) + exifPayloadLength];
        segment[0] = 0xff;
        segment[1] = 0xe1;
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(exifPayloadLength + sizeof(ushort)));

        var payload = segment.AsSpan(4);
        ExifIdentifier.CopyTo(payload);
        tiffPayload.AsSpan().CopyTo(payload[ExifIdentifier.Length..]);
        return segment;
    }

    private static byte[] CreateExifTiffPayload()
    {
        var tiff = new byte[33];
        tiff[0] = (byte)'I';
        tiff[1] = (byte)'I';
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(2), 42);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(4), 8);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(10), 0x0131);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(12), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(14), 7);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(18), 26);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(22), 0);
        Encoding.ASCII.GetBytes("Aprint\0").AsSpan().CopyTo(tiff.AsSpan(26));
        return tiff;
    }
}