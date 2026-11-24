namespace HealthyFac.Seeder.SeedFiles;

/// <summary>One failure of the seed check: the file, the item, and the broken rule.</summary>
public sealed class SeedCheckFailure
{
    public required string File { get; init; }

    public required string Item { get; init; }

    public required string Rule { get; init; }

    public override string ToString() => $"{File} | {Item} | {Rule}";
}
