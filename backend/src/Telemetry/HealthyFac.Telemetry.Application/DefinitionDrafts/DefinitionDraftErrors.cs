namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

/// <summary>
/// Defines error messages for the metric and status definition drafts.
/// </summary>
public static class DefinitionDraftErrors
{
    public const string DraftNotFound = "The definition draft does not exist.";
    public const string MachineNotRegistered = "The machine of the definition draft has no registration yet.";
    public const string DraftChanged = "The draft changed since you read it. Reload it and try again.";
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string MachineIdRequired = "MachineId is required.";
    public const string AttemptIdRequired = "The ingest attempt is required.";
    public const string DraftIdRequired = "The draft is required.";

    public const string DraftNotUsable =
        "The draft does not exist, belongs to another machine or kind, or is named more than once.";

    public const string KindUnknown = "The kind filter holds a kind that does not exist.";
    public const string SearchScopeRequired = "Give exactly one of the machine and the ingest attempt.";

    /// <summary>
    /// The stable key that the web app maps to a translated text. The dismiss and the definition writes return it
    /// with status 422 when the draft is not pending.
    /// </summary>
    public const string DraftNotPending = "draftNotPending";

    /// <summary>
    /// The stable key that the web app maps to a translated text. The apply of a phase draft returns it with status
    /// 422 while a condition or a limit row still names a metric or a status draft.
    /// </summary>
    public const string DraftUsesDrafts = "draftUsesDrafts";

    /// <summary>
    /// The stable key that the web app maps to a translated text. The dismiss of a metric or a status draft returns
    /// it with status 422 while a pending phase draft uses the draft.
    /// </summary>
    public const string DraftUsedByPhaseDraft = "draftUsedByPhaseDraft";
}
