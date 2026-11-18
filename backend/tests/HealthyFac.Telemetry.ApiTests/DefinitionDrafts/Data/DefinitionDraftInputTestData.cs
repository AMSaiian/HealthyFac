namespace HealthyFac.Telemetry.ApiTests.DefinitionDrafts.Data;

public static class DefinitionDraftInputTestData
{
    public const string DraftNotPendingKey = "draftNotPending";

    public const string DismissBody = "{}";

    public static string DismissPath(string encryptedDraftId) => $"v1/definition-drafts/{encryptedDraftId}/dismiss";
}
