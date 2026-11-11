namespace HealthyFac.Core.IntegrationTests.Database.Data;

public static class TransactionTestData
{
    public const string CompanyTimeZoneId = "Europe/Kyiv";
    public const string CommittedCompanyName = "Committed in transaction";
    public const string RolledBackCompanyName = "Rolled back in transaction";
    public const string UnsavedCompanyName = "Unsaved before transaction";
    public const string UnsavedAfterOperationCompanyName = "Unsaved after operation";
    public const string EndedTransactionCompanyName = "Ended by operation";
    public const string ShowIsolationSql = "SELECT current_setting('transaction_isolation') AS \"Value\"";
    public const string SerializableIsolation = "serializable";
    public const string ReadCommittedIsolation = "read committed";
}
