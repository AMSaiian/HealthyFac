namespace HealthyFac.Core.Application.Drafts;

/// <summary>
/// Defines validation limits of the draft use cases.
/// </summary>
public static class DraftValidationConstants
{
    /// <summary>The most draft IDs that one mark-applied call accepts.</summary>
    public const int MarkAppliedMaxIds = 200;
}
