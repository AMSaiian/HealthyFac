using HealthyFac.Gateway.Web.Proxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

public sealed class GatewayExceptionHandlerTests
{
    private const string SecretDetail = "secret exception detail";

    private readonly GatewayExceptionHandler _handler = new(NullLogger<GatewayExceptionHandler>.Instance);

    [Fact]
    public async Task TryHandle_WithException_WritesProblemDetailsWithoutDetails()
    {
        var context = CreateContext(CancellationToken.None);

        var isHandled = await _handler.TryHandleAsync(context, new InvalidOperationException(SecretDetail), default);

        var body = await ReadBodyAsync(context);

        isHandled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().StartWith(GatewayProblemDetailsConstants.MediaType);
        body.Should().Contain(GatewayProblemDetailsConstants.InternalServerError.Title);
        body.Should().NotContain(SecretDetail);
    }

    [Fact]
    public async Task TryHandle_WhenClientAborted_WritesNoBody()
    {
        using var abortedSource = new CancellationTokenSource();
        await abortedSource.CancelAsync();
        var context = CreateContext(abortedSource.Token);

        var isHandled = await _handler.TryHandleAsync(context, new OperationCanceledException(), default);

        var body = await ReadBodyAsync(context);

        isHandled.Should().BeTrue();
        body.Should().BeEmpty();
    }

    private static DefaultHttpContext CreateContext(CancellationToken requestAborted) =>
        new()
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            RequestAborted = requestAborted,
            Response = { Body = new MemoryStream() },
        };

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;

        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }
}
