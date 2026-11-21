namespace HealthyFac.Core.Application.Realtime;

public sealed record ProvisioningFailedData
{
    public required string EmployeeName { get; init; }

    public required string Reason { get; init; }
}
