namespace HealthyFac.Insights.IntegrationTests.Conversion;

public static class DoclingConversionTestData
{
    public static TheoryData<DoclingSampleFile> Files => new()
    {
        {
            new DoclingSampleFile(
                FileName: "sample-text.pdf", Extension: "pdf", ExpectedPageCount: 2, KnownWord: "pressure",
                HasLocation: true)
        },
        {
            new DoclingSampleFile(
                FileName: "sample.docx", Extension: "docx", ExpectedPageCount: 1, KnownWord: "pressure",
                HasLocation: true)
        },
        {
            new DoclingSampleFile(
                FileName: "sample.xlsx", Extension: "xlsx", ExpectedPageCount: 1, KnownWord: "pressure",
                HasLocation: true)
        },
        {
            new DoclingSampleFile(
                FileName: "sample.pptx", Extension: "pptx", ExpectedPageCount: 1, KnownWord: "pressure",
                HasLocation: true)
        },
        {
            new DoclingSampleFile(
                FileName: "sample.txt", Extension: "txt", ExpectedPageCount: 1, KnownWord: "pressure",
                HasLocation: false)
        },
        {
            new DoclingSampleFile(
                FileName: "sample.csv", Extension: "csv", ExpectedPageCount: 1, KnownWord: "pressure",
                HasLocation: false)
        },
        {
            new DoclingSampleFile(
                FileName: "sample.md", Extension: "md", ExpectedPageCount: 1, KnownWord: "pressure", HasLocation: true)
        },
    };
}

/// <summary>
/// One sample file of the Docling test: the expected page count, a word that must appear in the text, and whether
/// the chunks carry a location (the pages and the heading path).
/// </summary>
/// <param name="FileName">The file name in the sample folder.</param>
/// <param name="Extension">The extension without the dot.</param>
/// <param name="ExpectedPageCount">The page count that the converter reports.</param>
/// <param name="KnownWord">A word of the sample text.</param>
/// <param name="HasLocation">Whether at least one chunk has a location.</param>
public sealed record DoclingSampleFile(
    string FileName,
    string Extension,
    int ExpectedPageCount,
    string KnownWord,
    bool HasLocation
);
