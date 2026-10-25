namespace HealthyFac.Core.IntegrationTests.Database.Data;

public static class TransactionTestData
{
    public const string CommittedCompanyName = "Committed in transaction";
    public const string RolledBackCompanyName = "Rolled back in transaction";
    public const string UnsavedCompanyName = "Unsaved before transaction";
    public const string UnsavedAfterOperationCompanyName = "Unsaved after operation";
    public const string EndedTransactionCompanyName = "Ended by operation";
}
