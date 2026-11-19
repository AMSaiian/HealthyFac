using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.Persistence;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Shared.UnitTests.Conventions.Data;

public sealed record PlantedInboxMessage : IIntegrationMessage, IRequest
{
    public static string MessageType => "planted.v1:InboxMessage";
}

public sealed record PlantedPlainRequest : IRequest;

public sealed class PlantedInboxHandler(ITransactionalDbContext dbContext) : RequestHandler<PlantedInboxMessage>
{
    private readonly ITransactionalDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        PlantedInboxMessage request,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.RunInTransactionAsync(
            async token => { await Task.Delay(TimeSpan.Zero, token); },
            cancellationToken);
    }
}

public sealed class PlantedRequestHandler(ITransactionalDbContext dbContext) : RequestHandler<PlantedPlainRequest>
{
    private readonly ITransactionalDbContext _dbContext = dbContext;

    protected override Task HandleInternalAsync(PlantedPlainRequest request, CancellationToken cancellationToken) =>
        _dbContext.RunInTransactionAsync(
            async token => { await Task.Delay(TimeSpan.Zero, token); },
            cancellationToken);
}

public sealed class PlantedCleanInboxHandler : RequestHandler<PlantedInboxMessage>
{
    protected override Task HandleInternalAsync(PlantedInboxMessage request, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
