using FluentValidation;
using HealthyFac.Shared.Application.UseCases;

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

    public void Record() => RunCount++;
}

public sealed class NoResultRequestHandler(HandlerProbe probe) : RequestHandler<NoResultRequest>
{
    protected override Task HandleInternalAsync(NoResultRequest request, CancellationToken ct)
    {
        probe.Record();

        return Task.CompletedTask;
    }
}

public sealed record DuplicatedRequest : IRequest<int>;

public sealed class FirstDuplicatedRequestHandler : IRequestHandler<DuplicatedRequest, int>
{
    public Task<int> HandleAsync(DuplicatedRequest request, CancellationToken ct = default) => Task.FromResult(1);
}

public sealed class SecondDuplicatedRequestHandler : IRequestHandler<DuplicatedRequest, int>
{
    public Task<int> HandleAsync(DuplicatedRequest request, CancellationToken ct = default) => Task.FromResult(2);
}
