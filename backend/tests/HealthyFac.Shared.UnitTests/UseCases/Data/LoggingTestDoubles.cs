using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.UnitTests.UseCases.Data;

public sealed record TestRequest : IRequest<int>;

public sealed class ThrowingHandler : IRequestHandler<TestRequest, int>
{
    private readonly Exception _exception;

    public ThrowingHandler(Exception exception)
    {
        _exception = exception;
    }

    public Task<int> HandleAsync(TestRequest request, CancellationToken ct = default)
        => throw _exception;
}

public sealed class ErrorLevelRejectionException : Exception, IRejection
{
    public LogLevel? Level => LogLevel.Error;
}
