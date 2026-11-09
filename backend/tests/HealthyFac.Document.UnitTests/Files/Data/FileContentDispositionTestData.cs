namespace HealthyFac.Document.UnitTests.Files.Data;

public static class FileContentDispositionTestData
{
    public static TheoryData<string, string, string> AttachmentNames => new()
    {
        { "report", "pdf", "attachment; filename=report.pdf; filename*=utf-8''report.pdf" },
    };

    public static TheoryData<string> HostileNames => new()
    {
        { "a\"b" },
        { "line\r\nInjected: yes" },
        { "slash\\name" },
        { "per%cent" },
        { "помпа" },
    };
}
