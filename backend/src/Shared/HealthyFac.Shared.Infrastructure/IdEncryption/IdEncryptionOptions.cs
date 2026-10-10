namespace HealthyFac.Shared.Infrastructure.IdEncryption;

/// <summary>
/// Configures the identifier encryption keys. Keys are always supplied as an array, even when only one
/// key exists, so adding a second key later needs no configuration reshape.
/// </summary>
public sealed class IdEncryptionOptions
{
    public const string SectionName = "IdEncryption";

    public List<IdEncryptionKeyOptions> Keys { get; init; } = [];
}
