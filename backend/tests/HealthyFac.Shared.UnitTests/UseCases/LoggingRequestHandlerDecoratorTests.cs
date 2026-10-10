using HealthyFac.Shared.Application.UseCases.Decorators;
using HealthyFac.Shared.UnitTests.UseCases.Data;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.UnitTests.UseCases;

public sealed class LoggingRequestHandlerDecoratorTests
{
    [Theory]
    [MemberData(
        nameof(LoggingDecoratorTestData.RejectionsWithLevel),
        MemberType = typeof(LoggingDecoratorTestData))]
    public async Task HandleAsync_WhenHandlerThrowsRejection_LogsAtDeclaredLevelWithoutStackTrace(
        Exception rejection,
        LogLevel expectedLevel
    )
    {
        var logger = new CapturingLogger<LoggingRequestHandlerDecorator<TestRequest, int>>();
        var decorator = new LoggingRequestHandlerDecorator<TestRequest, int>(new ThrowingHandler(rejection), logger);

        var act = () => decorator.HandleAsync(new TestRequest());

        await act.Should().ThrowAsync<Exception>();
        var entry = logger.Entries.Should().ContainSingle(e => e.Message.Contains("rejected")).Subject;
        entry.Level.Should().Be(expectedLevel);
        entry.Exception.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(LoggingDecoratorTestData.IgnoredRejections), MemberType = typeof(LoggingDecoratorTestData))]
    public async Task HandleAsync_WhenRejectionHasNullLevel_LogsNoRejectionEntry(Exception rejection)
    {
        var logger = new CapturingLogger<LoggingRequestHandlerDecorator<TestRequest, int>>();
        var decorator = new LoggingRequestHandlerDecorator<TestRequest, int>(new ThrowingHandler(rejection), logger);

        var act = () => decorator.HandleAsync(new TestRequest());

        await act.Should().ThrowAsync<Exception>();
        logger.Entries.Should().NotContain(e => e.Message.Contains("rejected"));
        logger.Entries.Should().NotContain(e => e.Exception != null);
    }

    [Fact]
    public async Task HandleAsync_WhenHandlerThrowsUnexpectedException_LogsDebugWithoutExceptionAndNoRejection()
    {
        var failure = new InvalidOperationException("broken");
        var logger = new CapturingLogger<LoggingRequestHandlerDecorator<TestRequest, int>>();
        var decorator = new LoggingRequestHandlerDecorator<TestRequest, int>(new ThrowingHandler(failure), logger);

        var act = () => decorator.HandleAsync(new TestRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        logger.Entries.Should().NotContain(e => e.Message.Contains("rejected"));
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Debug && e.Message.Contains("failed"));
        logger.Entries.Should().NotContain(e => e.Exception != null);
    }
}
