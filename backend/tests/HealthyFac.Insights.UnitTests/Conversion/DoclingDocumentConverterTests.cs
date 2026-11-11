using System.Text;
using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Infrastructure.Ingest;
using HealthyFac.Insights.Infrastructure.Conversion;
using HealthyFac.Insights.UnitTests.Conversion.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.UnitTests.Conversion;

public sealed class DoclingDocumentConverterTests
{
    private const string PdfExtension = "pdf";

    private readonly FakeDoclingServer _server = new();
    private readonly ImmediateTimeProvider _time = new();
    private readonly IDocumentConverter _converter;

    public DoclingDocumentConverterTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{DoclingOptions.SectionName}:BaseUrl"] = DoclingTestData.BaseUrl,
                    [$"{DoclingOptions.SectionName}:ApiKey"] = DoclingTestData.ApiKey,
                    [$"{DoclingOptions.SectionName}:PollSeconds"] = DoclingTestData.PollSeconds.ToString(),
                    [$"{DoclingOptions.SectionName}:MaxPages"] = DoclingTestData.MaxPages.ToString(),
                })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(_time);
        services.AddSingleton<IProviderCalls>(
            new ProviderResilience(
                Options.Create(new IngestProcessingOptions { RetryDelayMilliseconds = 1 }),
                _time));
        services.AddDoclingConverter(configuration);
        services.AddHttpClient(DoclingConstants.ClientName).ConfigurePrimaryHttpMessageHandler(() => _server);
        _converter = services.BuildServiceProvider().GetRequiredService<IDocumentConverter>();
    }

    [Fact]
    public async Task Convert_AfterSeveralPolls_MapsTheChunksAndThePageCount()
    {
        _server.ScriptStatuses("started", "started", "success");
        _server.ResultJson = DoclingTestData.ThreePageResult;

        var document = await ConvertAsync(_ => Task.FromResult(result: false));

        document.PageCount.Should().Be(expected: 3);
        document.Chunks.Should().HaveCount(expected: 2);
        document.Chunks[index: 0].Text.Should().Be(expected: "Operating limits.");
        document.Chunks[index: 0].Location.Should().Be(expected: "pages 1-2: Manual > Limits");
        document.Chunks[index: 1].Location.Should().Be(expected: "Manual > Pressure");
    }

    [Fact]
    public async Task Convert_AfterSeveralPolls_WaitsThePollIntervalBetweenPolls()
    {
        _server.ScriptStatuses("started", "started", "success");
        _server.ResultJson = DoclingTestData.NoPagesResult;

        await ConvertAsync(_ => Task.FromResult(result: false));

        var polls = _server.Calls.Count(call => call.Path.StartsWith(value: "/v1/status/poll"));
        polls.Should().Be(expected: 3);
        _time.Delays.Should()
            .Equal(TimeSpan.FromSeconds(DoclingTestData.PollSeconds), TimeSpan.FromSeconds(seconds: 2));
    }

    [Fact]
    public async Task Convert_WithFileWithoutPages_CountsOnePage()
    {
        _server.ResultJson = DoclingTestData.NoPagesResult;

        var document = await ConvertAsync(_ => Task.FromResult(result: false));

        document.PageCount.Should().Be(expected: 1);
        document.Chunks.Should().ContainSingle().Which.Location.Should().BeNull();
    }

    [Fact]
    public async Task Convert_ForPresentation_LabelsTheLocationAsSlide()
    {
        _server.ResultJson = DoclingTestData.SlideResult;

        var document = await ConvertAsync(_ => Task.FromResult(result: false), extension: "pptx");

        document.Chunks.Should().ContainSingle().Which.Location.Should().Be(expected: "slide 4: Deck");
    }

    [Fact]
    public async Task Convert_WithNonSeekableStream_StreamsTheContentAndSendsTheOptions()
    {
        _server.ResultJson = DoclingTestData.NoPagesResult;

        await ConvertAsync(_ => Task.FromResult(result: false));

        var submit = _server.Calls.Single(call => call.Method == HttpMethod.Post.Method);
        submit.Body.Should().Contain(DoclingTestData.FileContent);
        submit.Body.Should().Contain(expected: "filename=manual.pdf");
        submit.Body.Should().Contain(expected: "easyocr");
        submit.Body.Should().Contain(expected: "name=chunking_max_tokens");
        submit.Body.Should().Contain(expected: "name=convert_page_range");
        submit.Body.Should().Contain((DoclingTestData.MaxPages + 1).ToString());
    }

    [Fact]
    public async Task Convert_Always_SendsTheApiKeyOnEveryCall()
    {
        _server.ResultJson = DoclingTestData.NoPagesResult;

        await ConvertAsync(_ => Task.FromResult(result: false));

        _server.Calls.Should().NotBeEmpty();
        _server.Calls.Should().OnlyContain(call => call.ApiKey == DoclingTestData.ApiKey);
    }

    [Fact]
    public async Task Convert_WhenTheTaskFails_ThrowsWithTheReasonOfDocling()
    {
        _server.ErrorMessage = "Unsupported page layout";
        _server.ScriptStatuses("started", "failure");

        var act = () => ConvertAsync(_ => Task.FromResult(result: false));

        await act.Should()
            .ThrowAsync<ConversionFailedException>()
            .WithMessage(expectedWildcardPattern: "Unsupported page layout");
    }

    [Fact]
    public async Task Convert_WhenTheUserCancelsDuringPolling_StopsBeforeTheNextPoll()
    {
        var asked = 0;
        _server.ScriptStatuses("started", "started", "started");

        var act = () => ConvertAsync(_ => Task.FromResult(++asked > 1));

        await act.Should().ThrowAsync<OperationCanceledException>();
        _server.Calls.Count(call => call.Path.StartsWith(value: "/v1/status/poll")).Should().Be(expected: 1);
        _server.Calls.Should().NotContain(call => call.Path.StartsWith("/v1/result"));
    }

    [Fact]
    public async Task Convert_WhenOnePollAnswers503_RepeatsOnlyThePollAndUploadsOnce()
    {
        _server.ScriptStatuses("started", "success");
        _server.PollFailures = 1;
        _server.ResultJson = DoclingTestData.NoPagesResult;

        var document = await ConvertAsync(_ => Task.FromResult(result: false));

        document.Chunks.Should().NotBeNull();
        _server.Calls.Count(call => call.Path.StartsWith(value: "/v1/chunk")).Should().Be(expected: 1);
        _server.Calls.Count(call => call.Path.StartsWith(value: "/v1/status/poll")).Should().Be(expected: 3);
    }

    [Fact]
    public async Task Convert_WhenTheSubmitAnswers503_OpensTheContentAgainAndUploadsAgain()
    {
        _server.SubmitFailures = 1;
        _server.ResultJson = DoclingTestData.NoPagesResult;
        var opened = 0;

        await _converter.ConvertAsync(
            _ =>
            {
                opened++;

                var bytes = Encoding.UTF8.GetBytes(DoclingTestData.FileContent);

                return Task.FromResult<Stream>(new NonSeekableStream(bytes));
            },
            fileName: "manual",
            PdfExtension,
            _ => Task.FromResult(result: false),
            TestContext.Current.CancellationToken);

        opened.Should().Be(expected: 2);
        _server.Calls.Count(call => call.Path.StartsWith(value: "/v1/chunk")).Should().Be(expected: 2);
    }

    [Fact]
    public async Task Convert_WhenTheResultReadAnswers503_RepeatsOnlyTheRead()
    {
        _server.ResultFailures = 1;
        _server.ResultJson = DoclingTestData.NoPagesResult;

        await ConvertAsync(_ => Task.FromResult(result: false));

        _server.Calls.Count(call => call.Path.StartsWith(value: "/v1/chunk")).Should().Be(expected: 1);
        _server.Calls.Count(call => call.Path.StartsWith(value: "/v1/result")).Should().Be(expected: 2);
    }

    private Task<ConvertedDocument> ConvertAsync(
        Func<CancellationToken, Task<bool>> isCancelled,
        string extension = PdfExtension
    ) =>
        _converter.ConvertAsync(
            _ => Task.FromResult<Stream>(new NonSeekableStream(Encoding.UTF8.GetBytes(DoclingTestData.FileContent))),
            fileName: "manual",
            extension,
            isCancelled,
            TestContext.Current.CancellationToken);
}
