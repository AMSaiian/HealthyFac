using System.Net;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using HealthyFac.Shared.UnitTests.HttpFailures.Data;

namespace HealthyFac.Shared.UnitTests.HttpFailures;

public sealed class HttpResponseMessageExtensionsTests
{
    [Fact]
    public async Task EnsureSuccessAsync_WithSuccess_ReadsNoBody()
    {
        using var response = HttpResponseFactory.Create(
            HttpStatusCode.OK,
            HttpFailureTestData.RequestUrl,
            content: new ThrowingContent());

        var act = () => response.EnsureSuccessAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureSuccessAsync_WithProblemDetails_PutsReasonInMessage()
    {
        using var response = HttpResponseFactory.Create(
            HttpStatusCode.Conflict,
            HttpFailureTestData.RequestUrl,
            $$"""{"detail":"{{HttpFailureTestData.ProblemDetail}}"}""");

        var act = () => response.EnsureSuccessAsync(CancellationToken.None);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.Message.Should()
            .Contain(HttpMethod.Post.Method)
            .And.Contain(HttpFailureTestData.RequestPath)
            .And.Contain(expected: "409")
            .And.Contain(nameof(HttpStatusCode.Conflict))
            .And.Contain(HttpFailureTestData.ProblemDetail);
    }

    [Fact]
    public async Task EnsureSuccessAsync_WithBodyNotJson_ThrowsWithoutReason()
    {
        using var response = HttpResponseFactory.Create(
            HttpStatusCode.GatewayTimeout,
            HttpFailureTestData.RequestUrl,
            HttpFailureTestData.HtmlBody);

        var act = () => response.EnsureSuccessAsync(CancellationToken.None);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.Message.Should()
            .Contain(expected: "504")
            .And.NotContain(HttpFailureTestData.HtmlText)
            .And.NotContain(unexpected: "html");
    }

    [Fact]
    public async Task EnsureSuccessAsync_WithCutInsideReason_ThrowsWithoutReason()
    {
        using var response = HttpResponseFactory.Create(
            HttpStatusCode.BadRequest,
            HttpFailureTestData.RequestUrl,
            HttpFailureTestData.LongBody());

        var act = () => response.EnsureSuccessAsync(CancellationToken.None);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.Message.Should()
            .Contain(expected: "400")
            .And.NotContain(new string(HttpFailureTestData.KeptCharacter, count: 10));
    }

    [Fact]
    public async Task EnsureSuccessAsync_WithBodyOverLimitAndReasonBeforeCut_ReadsReason()
    {
        using var response = HttpResponseFactory.Create(
            HttpStatusCode.BadRequest,
            HttpFailureTestData.RequestUrl,
            HttpFailureTestData.BodyWithReasonBeforeCut());

        var act = () => response.EnsureSuccessAsync(CancellationToken.None);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.Message.Should()
            .Contain(expected: "400")
            .And.Contain(HttpFailureTestData.ProblemDetail)
            .And.NotContain(new string(HttpFailureTestData.KeptCharacter, count: 10));
    }

    [Fact]
    public async Task EnsureSuccessAsync_WithReasonAfterCut_ThrowsWithoutReason()
    {
        using var response = HttpResponseFactory.Create(
            HttpStatusCode.BadRequest,
            HttpFailureTestData.RequestUrl,
            HttpFailureTestData.BodyWithReasonAfterCut());

        var act = () => response.EnsureSuccessAsync(CancellationToken.None);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.Message.Should()
            .Contain(expected: "400")
            .And.NotContain(HttpFailureTestData.ProblemDetail)
            .And.NotContain(new string(HttpFailureTestData.KeptCharacter, count: 10));
    }

    [Fact]
    public async Task EnsureSuccessAsync_WithReasonOverLimit_CutsReason()
    {
        using var response = HttpResponseFactory.Create(
            HttpStatusCode.BadRequest,
            HttpFailureTestData.RequestUrl,
            $$"""{"detail":"{{HttpFailureTestData.LongReason()}}"}""");

        var act = () => response.EnsureSuccessAsync(CancellationToken.None);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.Message.Should()
            .Contain(new string(HttpFailureTestData.KeptCharacter, HttpFailureConstants.MaxReasonLength))
            .And.NotContain(HttpFailureTestData.CutCharacter.ToString());
    }

    [Fact]
    public async Task EnsureSuccessAsync_WithQueryInUri_LeavesQueryOut()
    {
        using var response = HttpResponseFactory.Create(
            HttpStatusCode.NotFound,
            HttpFailureTestData.RequestUrlWithQuery);

        var act = () => response.EnsureSuccessAsync(CancellationToken.None);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.Message.Should()
            .Contain(HttpFailureTestData.RequestPath)
            .And.NotContain(HttpFailureTestData.QueryValue);
    }

    [Fact]
    public async Task EnsureSuccessAsync_WithFailure_KeepsStatusCode()
    {
        using var response = HttpResponseFactory.Create(HttpStatusCode.Conflict, HttpFailureTestData.RequestUrl);

        var act = () => response.EnsureSuccessAsync(CancellationToken.None);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
