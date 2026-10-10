namespace HealthyFac.Shared.Infrastructure.IdEncryption;

/// <summary>
/// Configures a specific ID encryption key and its associated version prefix.
/// </summary>
public sealed class IdEncryptionKeyOptions
{
    public required string Prefix { get; init; }

    public required string Key { get; init; }

    public bool Default { get; init; }
}
