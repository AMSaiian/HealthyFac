namespace HealthyFac.Shared.Application.Persistence.SoftDelete;

/// <summary>
/// Defines the exceptions of the set-based soft delete.
/// </summary>
public static class SoftDeleteErrors
{
    public static InvalidOperationException NoOpenTransaction() =>
        new(
            "A set-based soft delete runs only inside RunInTransactionAsync, "
            + "so it commits together with the save of the root."
        );
}
