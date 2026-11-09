using System.IO.Compression;
using System.Text;

namespace HealthyFac.Document.UnitTests.Files.Data;

/// <summary>
/// Builds minimal files in memory. A sample has the header and the structure that a signature check reads, and no
/// more. No sample is stored on disk.
/// </summary>
public static class SampleFiles
{
    private const string WordMain = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
    private const string WordMacroMain = "application/vnd.ms-word.document.macroEnabled.main+xml";
    private const string ExcelMain = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
    private const string ExcelMacroMain = "application/vnd.ms-excel.sheet.macroEnabled.main+xml";

    private const string PowerPointMain =
        "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml";

    private const string PowerPointMacroMain = "application/vnd.ms-powerpoint.presentation.macroEnabled.main+xml";

    private static readonly byte[] Padding = new byte[64];

    public static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. Padding];

    public static byte[] Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, .. Padding];

    public static byte[] Gif => [.. "GIF89a"u8, .. Padding];

    public static byte[] Webp => [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBP"u8, .. Padding];

    public static byte[] Pdf => [.. "%PDF-1.7\n"u8, .. Padding];

    public static byte[] Docx => Office(WordMain, mainPart: "word/document.xml");

    public static byte[] Xlsx => Office(ExcelMain, mainPart: "xl/workbook.xml");

    public static byte[] Pptx => Office(PowerPointMain, mainPart: "ppt/presentation.xml");

    public static byte[] Docm => Office(WordMacroMain, mainPart: "word/document.xml");

    public static byte[] Xlsm => Office(ExcelMacroMain, mainPart: "xl/workbook.xml");

    public static byte[] Pptm => Office(PowerPointMacroMain, mainPart: "ppt/presentation.xml");

    public static byte[] Zip => Archive(("readme.txt", "hello"));

    public static byte[] Svg => Encoding.UTF8.GetBytes(s: "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");

    public static byte[] Html => Encoding.UTF8.GetBytes(s: "<!DOCTYPE html><html><body>x</body></html>");

    public static byte[] Exe => [0x4D, 0x5A, 0x90, 0x00, .. Padding];

    public static byte[] Tiff => [0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, .. Padding];

    public static byte[] LegacyDoc => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, .. Padding];

    public static byte[] Utf8Text => Encoding.UTF8.GetBytes(s: "name,value\nпомпа,1\n");

    public static byte[] Utf8TextWithBom => [0xEF, 0xBB, 0xBF, .. Utf8Text];

    public static byte[] TextWithNul => [.. "abc"u8, 0x00, .. "def"u8];

    public static byte[] InvalidUtf8 => [.. "abc"u8, 0xC3, 0x28];

    private static byte[] Office(string mainContentType, string mainPart)
    {
        var contentTypes =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
            + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
            + $"<Override PartName=\"/{mainPart}\" ContentType=\"{mainContentType}\"/></Types>";

        return Archive(("[Content_Types].xml", contentTypes), (mainPart, "<root/>"));
    }

    private static byte[] Archive(params (string Name, string Content)[] entries)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(
                    entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(content);
            }
        }

        return stream.ToArray();
    }
}
