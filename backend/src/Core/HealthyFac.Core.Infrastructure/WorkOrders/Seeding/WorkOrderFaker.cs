using Bogus;
using HealthyFac.Core.Domain.WorkOrders;

namespace HealthyFac.Core.Infrastructure.WorkOrders.Seeding;

/// <summary>
/// Generates development work orders of one company. Every order starts in the backlog with an estimate of 60
/// minutes, so the seeder can lay several orders in one block without overlap.
/// </summary>
internal sealed class WorkOrderFaker : Faker<WorkOrder>
{
    public const int EstimateMinutes = 60;

    /// <summary>
    /// Declares the work order shape.
    /// </summary>
    /// <param name="companyId">The saved company that owns the orders.</param>
    /// <param name="machineIds">The machines to pick from. The order has no machine when the list is empty.</param>
    /// <param name="seed">The random seed, so every run produces the same orders.</param>
    public WorkOrderFaker(Guid companyId, IReadOnlyList<Guid> machineIds, int seed)
    {
        UseSeed(seed);
        CustomInstantiator(faker => WorkOrder.Create(
            companyId,
            faker.Hacker.Phrase(),
            faker.Lorem.Sentence(),
            faker.PickRandom<WorkOrderPriority>(WorkOrderPriority.All),
            faker.PickRandom<WorkOrderCategory>(WorkOrderCategory.All),
            machineIds.Count == 0 ? null : faker.PickRandom<Guid>(machineIds),
            TimeSpan.FromMinutes(EstimateMinutes)));
    }
}
