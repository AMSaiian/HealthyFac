namespace HealthyFac.Core.Application.Drafts;

/// <summary>
/// Defines error messages for the draft use cases.
/// </summary>
public static class DraftErrors
{
    public const string DraftNotFound = "The draft does not exist.";
    public const string DraftIdRequired = "The draft is required.";
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string AttemptIdRequired = "The ingest attempt is required.";
    public const string KindUnknown = "The kind filter holds a kind that does not exist.";
    public const string SearchScopeRequired = "Give exactly one of the machine and the ingest attempt.";
    public const string RowVersionRequired = "The row version of the machine is required to apply an update draft.";
    public const string DraftChanged = "The draft or the machine changed since you read it. Reload it and try again.";

    /// <summary>
    /// The stable key that the web app maps to a translated text. The apply and the dismiss return it with status 409
    /// when the draft is not pending.
    /// </summary>
    public const string DraftNotPending = "draftNotPending";
}
