namespace HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

/// <summary>
/// Defines the errors that the check constraint builder raises.
/// </summary>
public static class CheckConstraintErrors
{
    public static ArgumentException NameTooLong(string name) =>
        new(
            $"The check constraint name '{name}' has {name.Length} characters. "
            + $"PostgreSQL allows at most {CheckConstraintConstants.MaxIdentifierLength}.",
            nameof(name));
}
