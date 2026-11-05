using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Application.UseCases.Decorators;
using HealthyFac.Shared.UnitTests.UseCases.Data;
using HealthyFac.Testing.Logging;
using HealthyFac.Testing.Tracing;
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
        var failure = new InvalidOperationException(message: "broken");
        var logger = new CapturingLogger<LoggingRequestHandlerDecorator<TestRequest, int>>();
        var decorator = new LoggingRequestHandlerDecorator<TestRequest, int>(new ThrowingHandler(failure), logger);

        var act = () => decorator.HandleAsync(new TestRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        logger.Entries.Should().NotContain(e => e.Message.Contains("rejected"));
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Debug && e.Message.Contains("failed"));
        logger.Entries.Should().NotContain(e => e.Exception != null);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_LeavesSpanStatusUnsetAndRethrows()
    {
        using var capture = new SpanCapture(
            ObservabilityConstants.Sources.UseCases,
            LoggingDecoratorTestData.TestSourceName);
        using var source = new ActivitySource(LoggingDecoratorTestData.TestSourceName);
        using var parent = source.StartActivity(LoggingDecoratorTestData.ParentActivityName);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var decorator = new LoggingRequestHandlerDecorator<TestRequest, int>(
            new ThrowingHandler(new OperationCanceledException(cancellation.Token)),
            new CapturingLogger<LoggingRequestHandlerDecorator<TestRequest, int>>());

        var act = () => decorator.HandleAsync(new TestRequest(), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        capture.Spans.Should()
            .ContainSingle(span => IsUseCaseSpanOf(span, parent!))
            .Which.Status.Should()
            .Be(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task HandleAsync_WhenHandlerFails_MarksSpanStatusError()
    {
        using var capture = new SpanCapture(
            ObservabilityConstants.Sources.UseCases,
            LoggingDecoratorTestData.TestSourceName);
        using var source = new ActivitySource(LoggingDecoratorTestData.TestSourceName);
        using var parent = source.StartActivity(LoggingDecoratorTestData.ParentActivityName);
        var decorator = new LoggingRequestHandlerDecorator<TestRequest, int>(
            new ThrowingHandler(new InvalidOperationException()),
            new CapturingLogger<LoggingRequestHandlerDecorator<TestRequest, int>>());

        var act = () => decorator.HandleAsync(new TestRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        capture.Spans.Should()
            .ContainSingle(span => IsUseCaseSpanOf(span, parent!))
            .Which.Status.Should()
            .Be(ActivityStatusCode.Error);
    }

    private static bool IsUseCaseSpanOf(Activity span, Activity parent) =>
        span.Source.Name == ObservabilityConstants.Sources.UseCases && span.TraceId == parent.TraceId;
}
