namespace HealthyFac.Core.Domain.Drafts;

/// <summary>
/// The proposed values of a status definition draft. Every value can be <see langword="null"/>.
/// </summary>
public sealed record StatusDraftValues
{
    public string? SourceLocation { get; init; }
    public string? Code { get; init; }
    public string? Severity { get; init; }
    public string? Description { get; init; }
}
