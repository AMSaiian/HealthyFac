namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;

public static class SoftDeleteCascadeErrors
{
    public static InvalidOperationException NoOpenTransaction() => new(
        "A soft delete cascade runs only inside a transaction, so it commits with the save. "
        + "Override SaveChangesAsync with TransactionRunner.SaveAsync.");

    public static InvalidOperationException SynchronousSave() => new(
        message: "A save that cascades a soft delete must use SaveChangesAsync.");

    public static InvalidOperationException UnsupportedKey(string relation) => new(
        $"A soft delete cascade supports only a single Guid key column: {relation}.");
}
