namespace HealthyFac.Seeder.WorkOrders;

/// <summary>Chooses the planned start of an assignment at run time.</summary>
/// <param name="order">The work order.</param>
/// <param name="assigneeId">The employee who gets the order.</param>
/// <param name="context">The keys and the anchor of the job.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
/// <returns>The planned start as a local time of the company zone, on the grid of Core.</returns>
internal delegate Task<DateTime> PlannedStartChooser(
    WorkOrderSeed order,
    Guid assigneeId,
    Timeline.SeedContext context,
    CancellationToken cancellationToken
);
