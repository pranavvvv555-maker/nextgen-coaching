using System.IO.Compression;

namespace NextGenCoaching.Core.Services.Storage;

public static class ResumeFileValidation
{
    public static bool IsSupported(byte[] bytes, string extension)
    {
        if (extension == ".pdf") return bytes.AsSpan().StartsWith("%PDF-"u8);
        if (extension == ".doc") return bytes.AsSpan().StartsWith(new byte[] { 0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1 });
        if (extension != ".docx") return false;
        try
        {
            using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            return zip.GetEntry("[Content_Types].xml") is not null && zip.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException) { return false; }
    }
}
