namespace HealthyFac.Insights.IntegrationTests.Conversion;

[Collection(DoclingCollection.Name)]
public sealed class DoclingConversionTests(DoclingFixture fixture)
{
    private const string ScanFile = "sample-scan.pdf";
    private const string KnownScanWord = "OVERHEATING";
    private const string PumpWord = "pump";

    private static readonly string FolderPath = Path.Combine(AppContext.BaseDirectory, "Conversion", "Files");

    private readonly DoclingFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(DoclingConversionTestData.Files), MemberType = typeof(DoclingConversionTestData))]
    public async Task Convert_ForSampleFile_ReturnsTextChunksWithTheExpectedLocation(DoclingSampleFile sample)
    {
        await using var content = File.OpenRead(Path.Combine(FolderPath, sample.FileName));

        var document = await _fixture.Converter.ConvertAsync(
            _ => Task.FromResult<Stream>(content),
            Path.GetFileNameWithoutExtension(sample.FileName),
            sample.Extension,
            isCancelled: _ => Task.FromResult(false),
            TestContext.Current.CancellationToken);

        document.PageCount.Should().Be(sample.ExpectedPageCount);
        document.Chunks.Should().NotBeEmpty();
        document.Chunks.Should().OnlyContain(chunk => !string.IsNullOrWhiteSpace(chunk.Text));
        document.Chunks.Should().Contain(
            chunk => chunk.Text.Contains(sample.KnownWord, StringComparison.OrdinalIgnoreCase));
        document.Chunks.Any(chunk => chunk.Location != null).Should().Be(sample.HasLocation);
    }

    [Fact]
    public async Task Convert_ForImageOnlyPdf_ReadsThePrintedWordWithOcr()
    {
        await using var content = File.OpenRead(Path.Combine(FolderPath, ScanFile));

        var document = await _fixture.Converter.ConvertAsync(
            _ => Task.FromResult<Stream>(content),
            fileName: "sample-scan",
            extension: "pdf",
            isCancelled: _ => Task.FromResult(false),
            TestContext.Current.CancellationToken);

        document.Chunks.Should().Contain(
            chunk => chunk.Text.Contains(KnownScanWord, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Convert_ForTextPdf_UsesThePageNumbersInTheLocation()
    {
        await using var content = File.OpenRead(Path.Combine(FolderPath, "sample-text.pdf"));

        var document = await _fixture.Converter.ConvertAsync(
            _ => Task.FromResult<Stream>(content),
            fileName: "sample-text",
            extension: "pdf",
            isCancelled: _ => Task.FromResult(false),
            TestContext.Current.CancellationToken);

        document.Chunks.Should().Contain(chunk => chunk.Location != null && chunk.Location.StartsWith("page 1"));
        document.Chunks.Should().Contain(chunk => chunk.Location != null && chunk.Location.StartsWith("page 2"));
        document.Chunks.Should().Contain(chunk => chunk.Text.Contains(PumpWord, StringComparison.OrdinalIgnoreCase));
    }
}
