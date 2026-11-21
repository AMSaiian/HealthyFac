using System.Diagnostics;
using HealthyFac.Shared.Application;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Infrastructure;
using HealthyFac.Shared.Web.Realtime;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.UnitTests.Conventions.Data;

public sealed class PlantedInterfaceItem
{
    public required IDisposable Resource { get; init; }

    public required IReadOnlyList<string> Names { get; init; }

    public required DbContext Context { get; init; }

    public required IServiceProvider Services { get; init; }

    public required string Name { get; init; }
}

public sealed class PlantedSharedItem
{
    public required string Name { get; init; }
}

public sealed class PlantedCleanItem
{
    public required string Name { get; init; }

    public required Guid[] Ids { get; init; }
}

public abstract class PlantedItemHandler<TItem> : IBackgroundItemHandler<TItem>
{
    public Task HandleAsync(TItem item, ActivityContext parentContext, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class PlantedInterfaceItemHandler : PlantedItemHandler<PlantedInterfaceItem>;

public sealed class PlantedFirstSharedItemHandler : PlantedItemHandler<PlantedSharedItem>;

public sealed class PlantedSecondSharedItemHandler : PlantedItemHandler<PlantedSharedItem>;

public sealed class PlantedCleanItemHandler : PlantedItemHandler<PlantedCleanItem>;

public static class BackgroundItemConventionTestData
{
    public static readonly Type[] SourceAssemblyLabels =
    [
        typeof(SharedApplicationAssembly),
        typeof(SharedInfrastructureAssembly),
        typeof(RealtimeHub),
    ];
}
