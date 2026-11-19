namespace HealthyFac.Insights.Application.Agents;

/// <summary>
/// The payload of a buffered <c>proposeWorkOrder</c> action: the signals that the proposal covers and the values of
/// the work order.
/// </summary>
public sealed record ProposeWorkOrderPayload
{
    public required IReadOnlyList<Guid> SignalIds { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required string Priority { get; init; }

    public required string Category { get; init; }

    public required int EstimateMinutes { get; init; }
}

/// <summary>The payload of a buffered <c>linkWorkOrder</c> action.</summary>
public sealed record LinkWorkOrderPayload
{
    public required IReadOnlyList<Guid> SignalIds { get; init; }

    public required Guid WorkOrderId { get; init; }
}

/// <summary>The payload of a buffered <c>closeCase</c> action.</summary>
public sealed record CloseCasePayload
{
    public required string Reason { get; init; }
}
