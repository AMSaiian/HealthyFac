namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;

public static class SoftDeleteCascadeErrors
{
    public static InvalidOperationException NoOpenTransaction() => new(
        "A soft delete cascade runs only inside a transaction, so it commits with the save. "
        + "Override SaveChangesAsync and SaveChanges with TransactionRunner.SaveAsync and TransactionRunner.Save.");

    public static InvalidOperationException UnsupportedKey(string relation) => new(
        $"A soft delete cascade supports only a single Guid key column: {relation}.");
}
