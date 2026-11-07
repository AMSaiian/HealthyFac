using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Telemetry.UnitTests.Database.Data;

public static class EntityBaseModelTestData
{
    /// <summary>
    /// The entity types without <c>IEntity</c> on purpose, by CLR type name: the messaging tables, and every reading
    /// type with a composite natural key that nothing references by a single row. A new exempt type needs a
    /// deliberate entry here.
    /// </summary>
    public static readonly IReadOnlyList<string> ExemptEntityNames =
    [
        "OutboxMessage",
        "InboxMessage",
        nameof(NumericReading),
        nameof(DiscreteReading),
        nameof(StatusReading),
    ];
}
