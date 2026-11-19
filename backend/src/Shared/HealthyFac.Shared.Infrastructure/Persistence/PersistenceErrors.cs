using System.Data;

namespace HealthyFac.Shared.Infrastructure.Persistence;

/// <summary>
/// Defines the exceptions that the service contexts throw when they run an operation in a transaction.
/// </summary>
public static class PersistenceErrors
{
    public static InvalidOperationException UnsavedChangesBeforeTransaction() =>
        new(
            "The context has unsaved changes before the transaction starts. "
            + "Save them first, or make every change inside the transaction operation, "
            + "because each attempt clears the change tracker."
        );

    public static InvalidOperationException UnsavedChangesAfterOperation() =>
        new(
            "The transaction operation left unsaved changes. "
            + "Call SaveChangesAsync inside the operation, because the context commits only saved changes."
        );

    public static InvalidOperationException TransactionEndedByOperation() =>
        new(
            "The transaction operation committed or rolled back the transaction. "
            + "Only the context ends the transaction, so a retry cannot repeat a committed part."
        );

    public static InvalidOperationException IsolationLevelDiffersFromOpenTransaction(
        IsolationLevel open,
        IsolationLevel requested
    ) =>
        new(
            $"The open transaction runs at {open}, so an operation at {requested} cannot join it. "
            + "Use the level of the open transaction."
        );
}
