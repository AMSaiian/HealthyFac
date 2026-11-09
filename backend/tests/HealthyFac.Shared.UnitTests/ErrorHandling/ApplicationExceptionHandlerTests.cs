using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Web.ErrorHandling;
using Microsoft.AspNetCore.Http;

namespace HealthyFac.Shared.UnitTests.ErrorHandling;

public sealed class ApplicationExceptionHandlerTests
{
    private const string Message = "The storage is down.";

    [Fact]
    public async Task TryHandleAsync_WithServiceUnavailableException_Writes503()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var handler = new ApplicationExceptionHandler();

        var isHandled = await handler.TryHandleAsync(
            context, new ServiceUnavailableException(Message), TestContext.Current.CancellationToken);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);

        isHandled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        body.Should().Contain(Message);
    }
}
