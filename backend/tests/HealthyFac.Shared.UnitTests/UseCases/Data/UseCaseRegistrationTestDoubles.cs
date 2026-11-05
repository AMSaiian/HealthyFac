using FluentValidation;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Shared.UnitTests.UseCases.Data;

public sealed record NoResultRequest : IRequest
{
    public required string Name { get; init; }
}

public sealed class NoResultRequestValidator : AbstractValidator<NoResultRequest>
{
    public NoResultRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty();
    }
}

public sealed class HandlerProbe
{
    public int RunCount { get; private set; }

    public int RuleRunCount { get; private set; }

    public void Record() => RunCount++;

    public void RecordRule() => RuleRunCount++;
}

public sealed class NoResultRequestPreHandlerCheck(HandlerProbe probe) : IPreHandlerCheck<NoResultRequest>
{
    private readonly HandlerProbe _probe = probe;

    public Task CheckAsync(NoResultRequest request, CancellationToken cancellationToken)
    {
        _probe.RecordRule();

        return Task.CompletedTask;
    }
}

public sealed record RuledRequest : IRequest<int>
{
    public required string Name { get; init; }

    public required bool IsRejected { get; init; }
}

public sealed class RuledRequestValidator : AbstractValidator<RuledRequest>
{
    public RuledRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty();
    }
}

public sealed class RuledRequestPreHandlerCheck(HandlerProbe probe) : IPreHandlerCheck<RuledRequest>
{
    private readonly HandlerProbe _probe = probe;

    public Task CheckAsync(RuledRequest request, CancellationToken cancellationToken)
    {
        _probe.RecordRule();

        return request.IsRejected
            ? throw new ForbiddenAccessException(message: "Outside the scope.")
            : Task.CompletedTask;
    }
}

public sealed class RuledRequestHandler(HandlerProbe probe) : IRequestHandler<RuledRequest, int>
{
    private readonly HandlerProbe _probe = probe;

    public Task<int> HandleAsync(
        RuledRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        _probe.Record();

        return Task.FromResult(result: 1);
    }
}

public sealed record UnruledRequest : IRequest<int>;

public sealed class UnruledRequestHandler : IRequestHandler<UnruledRequest, int>
{
    public Task<int> HandleAsync(
        UnruledRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        Task.FromResult(result: 1);
}

public sealed record DoubleRuledRequest : IRequest<int>;

public sealed class DoubleRuledRequestHandler : IRequestHandler<DoubleRuledRequest, int>
{
    public Task<int> HandleAsync(
        DoubleRuledRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        Task.FromResult(result: 1);
}

public sealed class FirstDoubleRuledPreHandlerCheck : PassThroughPreHandlerCheck<DoubleRuledRequest>;

public sealed class SecondDoubleRuledPreHandlerCheck : PassThroughPreHandlerCheck<DoubleRuledRequest>;

public sealed class NoResultRequestHandler(HandlerProbe probe) : RequestHandler<NoResultRequest>
{
    private readonly HandlerProbe _probe = probe;

    protected override Task HandleInternalAsync(NoResultRequest request, CancellationToken cancellationToken)
    {
        _probe.Record();

        return Task.CompletedTask;
    }
}

public sealed record DuplicatedRequest : IRequest<int>;

public sealed class FirstDuplicatedRequestHandler : IRequestHandler<DuplicatedRequest, int>
{
    public Task<int> HandleAsync(
        DuplicatedRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        Task.FromResult(result: 1);
}

public sealed class SecondDuplicatedRequestHandler : IRequestHandler<DuplicatedRequest, int>
{
    public Task<int> HandleAsync(
        DuplicatedRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        Task.FromResult(result: 2);
}
