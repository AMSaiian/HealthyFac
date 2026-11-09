namespace HealthyFac.Document.UnitTests.Files.Data;

public static class FileInspectorTestData
{
    public static TheoryData<string, byte[], string, string> AcceptedAttachments => new()
    {
        { "png", SampleFiles.Png, "png", "image/png" },
        { "jpg", SampleFiles.Jpeg, "jpg", "image/jpeg" },
        { "jpeg", SampleFiles.Jpeg, "jpeg", "image/jpeg" },
        { "gif", SampleFiles.Gif, "gif", "image/gif" },
        { "webp", SampleFiles.Webp, "webp", "image/webp" },
        { "pdf", SampleFiles.Pdf, "pdf", "application/pdf" },
        {
            "docx",
            SampleFiles.Docx,
            "docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        },
        {
            "xlsx",
            SampleFiles.Xlsx,
            "xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        },
        {
            "pptx",
            SampleFiles.Pptx,
            "pptx",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        },
        { "txt", SampleFiles.Utf8Text, "txt", "text/plain" },
        { "txt with charset", SampleFiles.Utf8Text, "txt", "text/plain; charset=utf-8" },
        { "txt with bom", SampleFiles.Utf8TextWithBom, "txt", "text/plain" },
        { "csv", SampleFiles.Utf8Text, "csv", "text/csv" },
        { "md", SampleFiles.Utf8Text, "md", "text/markdown" },
        { "md as octet stream", SampleFiles.Utf8Text, "md", "application/octet-stream" },
        { "csv without type", SampleFiles.Utf8Text, "csv", "" },
    };

    public static TheoryData<string, byte[], string, string> RefusedAttachments => new()
    {
        { "docm", SampleFiles.Docm, "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { "xlsm", SampleFiles.Xlsm, "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        {
            "pptm",
            SampleFiles.Pptm,
            "pptx",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        },
        { "legacy doc", SampleFiles.LegacyDoc, "docx", "application/msword" },
        { "zip", SampleFiles.Zip, "zip", "application/zip" },
        { "zip named docx", SampleFiles.Zip, "docx", "application/zip" },
        { "svg", SampleFiles.Svg, "svg", "image/svg+xml" },
        { "svg named png", SampleFiles.Svg, "png", "image/png" },
        { "html", SampleFiles.Html, "html", "text/html" },
        { "exe named pdf", SampleFiles.Exe, "pdf", "application/pdf" },
        { "tiff", SampleFiles.Tiff, "tiff", "image/tiff" },
        { "tiff named png", SampleFiles.Tiff, "png", "image/png" },
        { "text with nul", SampleFiles.TextWithNul, "txt", "text/plain" },
        { "invalid utf-8", SampleFiles.InvalidUtf8, "txt", "text/plain" },
        { "png named pdf", SampleFiles.Png, "pdf", "application/pdf" },
        { "pdf declared as png", SampleFiles.Pdf, "pdf", "image/png" },
        { "png without extension match", SampleFiles.Png, "jpg", "image/png" },
        { "pdf declared as octet stream", SampleFiles.Pdf, "pdf", "application/octet-stream" },
    };
}
