namespace HealthyFac.Document.UnitTests.Files.Data;

public static class FileNamesTestData
{
    public static TheoryData<string, string?> BaseNames => new()
    {
        { "Pump manual.v2.pdf", "Pump manual.v2" },
        { "report.pdf", "report" },
        { "C:\\docs\\a.pdf", "a" },
        { "/home/u/a.pdf", "a" },
        { ".pdf", null },
        { "noext", "noext" },
        { "a.", "a" },
    };

    public static TheoryData<string, string?> Extensions => new()
    {
        { "Pump manual.v2.PDF", "pdf" },
        { "C:\\docs.v1\\noext", null },
        { "report.", null },
        { "a.jpeg", "jpeg" },
    };

    public static TheoryData<string, string, string> StrippedNames => new()
    {
        { "report.pdf", "pdf", "report" },
        { "REPORT.PDF", "pdf", "REPORT" },
        { "report.docx", "pdf", "report.docx" },
        { "report", "pdf", "report" },
    };
}
