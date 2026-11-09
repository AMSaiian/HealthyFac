namespace HealthyFac.Document.UnitTests.Database.Data;

public static class EntityBaseModelTestData
{
    /// <summary>
    /// The entity types without <c>IEntity</c> on purpose, by CLR type name: the messaging tables. A new exempt
    /// type needs a deliberate entry here.
    /// </summary>
    public static readonly IReadOnlyList<string> ExemptEntityNames =
    [
        "OutboxMessage",
        "InboxMessage",
    ];
}
