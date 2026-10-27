namespace HealthyFac.Core.Application.Employees.InviteLinks;

public sealed class InviteLinkOptions
{
    public const string SectionName = "InviteLinks";

    public required string SpaBaseUrl { get; init; }
}
