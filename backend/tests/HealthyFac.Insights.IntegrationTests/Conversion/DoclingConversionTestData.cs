namespace HealthyFac.Insights.IntegrationTests.Conversion;

public static class DoclingConversionTestData
{
    public static TheoryData<DoclingSampleFile> Files => new()
    {
        { new DoclingSampleFile("sample-text.pdf", "pdf", ExpectedPageCount: 2, "pressure", HasLocation: true) },
        { new DoclingSampleFile("sample.docx", "docx", ExpectedPageCount: 1, "pressure", HasLocation: true) },
        { new DoclingSampleFile("sample.xlsx", "xlsx", ExpectedPageCount: 1, "pressure", HasLocation: true) },
        { new DoclingSampleFile("sample.pptx", "pptx", ExpectedPageCount: 1, "pressure", HasLocation: true) },
        { new DoclingSampleFile("sample.txt", "txt", ExpectedPageCount: 1, "pressure", HasLocation: false) },
        { new DoclingSampleFile("sample.csv", "csv", ExpectedPageCount: 1, "pressure", HasLocation: false) },
        { new DoclingSampleFile("sample.md", "md", ExpectedPageCount: 1, "pressure", HasLocation: true) },
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
