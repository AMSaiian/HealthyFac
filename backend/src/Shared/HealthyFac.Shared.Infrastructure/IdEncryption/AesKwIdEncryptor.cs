using System.Buffers.Text;
using System.Security.Cryptography;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.Observability;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace HealthyFac.Shared.Infrastructure.IdEncryption;

/// <summary>
/// Encrypts and decrypts Guid identifiers using the AES Key Wrap algorithm.
/// </summary>
public sealed class AesKwIdEncryptor : IIdEncryptor, IDisposable
{
    private readonly IDisposable? _subscription;
    private EncryptionState _state;

    /// <summary>
    /// Initializes a new encryptor instance monitored dynamically via options monitor.
    /// </summary>
    /// <param name="optionsMonitor">The options monitor providing encryption key configurations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsMonitor"/> is null.</exception>
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public AesKwIdEncryptor(IOptionsMonitor<IdEncryptionOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _state = BuildState(optionsMonitor.CurrentValue);
        _subscription = optionsMonitor.OnChange(OnConfigurationChanged);
    }

    /// <summary>
    /// Initializes a new encryptor instance using a single 256-bit AES encryption key.
    /// </summary>
    /// <param name="key">The 256-bit AES encryption key byte array.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> length does not equal 32
    /// bytes.</exception>
    public AesKwIdEncryptor(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _state = SingleVersionState(CreateVersion(IdEncryptionConstants.CurrentVersionPrefix, key));
    }

    /// <summary>
    /// Encrypts a Guid identifier into an encoded token string using the active default version.
    /// </summary>
    /// <param name="id">The plain <see cref="Guid"/> identifier to encrypt.</param>
    /// <returns>The URL-safe encrypted token string.</returns>
    public string Encrypt(Guid id)
    {
        using var activity = IdEncryptionActivitySource.Instance.StartActivity(ObservabilityConstants.Spans.Encrypt);

        return EncryptInternal(id);
    }

    /// <summary>
    /// Decrypts an encrypted token string into a <see cref="Guid"/> identifier or throws when invalid.
    /// </summary>
    /// <param name="encryptedId">The URL-safe encrypted token string to decrypt.</param>
    /// <returns>The decrypted <see cref="Guid"/> identifier.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="encryptedId"/> is null, empty, or
    /// whitespace.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="encryptedId"/> contains an unsupported version
    /// prefix, invalid base64 encoding, or invalid payload length.</exception>
    /// <exception cref="CryptographicException">Thrown when authentication tag validation fails or payload integrity is
    /// compromised.</exception>
    public Guid Decrypt(string encryptedId)
    {
        using var activity = IdEncryptionActivitySource.Instance.StartActivity(ObservabilityConstants.Spans.Decrypt);

        try
        {
            var id = DecryptInternal(encryptedId);
            activity.MarkSuccess();

            return id;
        }
        catch (Exception exception)
            when (exception is
                ArgumentException
                or FormatException
                or CryptographicException
            )
        {
            activity.MarkRejected();

            throw;
        }
        catch (Exception exception)
        {
            activity.MarkFailure(exception);

            throw;
        }
    }

    /// <summary>
    /// Re-encrypts an encrypted identifier token into the active default encryption version.
    /// </summary>
    /// <param name="encryptedId">The version-prefixed encrypted identifier token to migrate.</param>
    /// <returns>The migrated encrypted identifier token.</returns>
    public string Migrate(string encryptedId) => Encrypt(Decrypt(encryptedId));

    /// <inheritdoc/>
    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private string EncryptInternal(Guid id)
    {
        var currentDefault = _state.DefaultVersion;

        var engine = new AesWrapEngine();
        engine.Init(true, currentDefault.KeyParameter);

        var wrappedBytes = engine.Wrap(
            id.ToByteArray(bigEndian: true),
            0,
            IdEncryptionConstants.Lengths.PlaintextGuidBytes
        );

        return $"{currentDefault.Prefix}{Base64Url.EncodeToString(wrappedBytes)}";
    }

    private Guid DecryptInternal(string encryptedId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedId);

        var version = _state.FindVersion(encryptedId);
        if (version is null)
        {
            throw IdEncryptionErrors.UnsupportedVersionPrefix(encryptedId);
        }

        var token = encryptedId[version.Prefix.Length..];

        byte[] cipherBytes;
        try
        {
            cipherBytes = Base64Url.DecodeFromChars(token);
        }
        catch (FormatException ex)
        {
            throw IdEncryptionErrors.InvalidBase64UrlEncoding(ex);
        }

        if (cipherBytes.Length != IdEncryptionConstants.Lengths.ExpectedCiphertextBytes)
        {
            throw IdEncryptionErrors.UnexpectedCiphertextLength(cipherBytes.Length);
        }

        byte[] unwrappedBytes;
        try
        {
            var engine = new AesWrapEngine();
            engine.Init(false, version.KeyParameter);
            unwrappedBytes = engine.Unwrap(cipherBytes, 0, cipherBytes.Length);
        }
        catch (InvalidCipherTextException ex)
        {
            throw IdEncryptionErrors.AuthenticationFailed(ex);
        }

        return unwrappedBytes.Length != IdEncryptionConstants.Lengths.PlaintextGuidBytes
            ? throw IdEncryptionErrors.PayloadLengthMismatch()
            : new Guid(unwrappedBytes, bigEndian: true);
    }

    private void OnConfigurationChanged(IdEncryptionOptions newOptions)
    {
        _state = BuildState(newOptions);
    }

    private static EncryptionState BuildState(IdEncryptionOptions options)
    {
        if (options.Keys.Count <= 0)
        {
            throw IdEncryptionErrors.NoKeysConfigured(IdEncryptionOptions.SectionName);
        }

        var versions = new List<EncryptionVersion>(options.Keys.Count);
        EncryptionVersion? defaultVersion = null;

        foreach (var keyOptions in options.Keys)
        {
            var version = CreateVersion(keyOptions.Prefix, Convert.FromBase64String(keyOptions.Key));
            versions.Add(version);

            if (keyOptions.Default)
            {
                defaultVersion = version;
            }
        }

        if (defaultVersion is null)
        {
            throw IdEncryptionErrors.NoDefaultKeyConfigured();
        }

        return new EncryptionState
        {
            DefaultVersion = defaultVersion,
            Versions = versions.OrderByDescending(version => version.Prefix.Length).ToList(),
        };
    }

    /// <summary>
    /// Validates the key length and builds the encryption version for a prefix.
    /// This is the single key validation path for every entry point.
    /// </summary>
    /// <param name="prefix">The version prefix that identifies tokens produced by this key.</param>
    /// <param name="keyBytes">The decoded AES key bytes.</param>
    /// <returns>The encryption version for <paramref name="prefix"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="keyBytes"/> length does not equal 32 bytes.
    /// </exception>
    private static EncryptionVersion CreateVersion(string prefix, byte[] keyBytes)
    {
        if (keyBytes.Length != IdEncryptionConstants.Lengths.RequiredKeyBytes)
        {
            throw IdEncryptionErrors.InvalidKeyLength(keyBytes.Length);
        }

        return new EncryptionVersion
        {
            Prefix = prefix,
            KeyParameter = new KeyParameter(keyBytes),
        };
    }

    private static EncryptionState SingleVersionState(EncryptionVersion version)
        => new()
        {
            DefaultVersion = version,
            Versions = [version],
        };

    private sealed class EncryptionVersion
    {
        public required string Prefix { get; init; }
        public required KeyParameter KeyParameter { get; init; }
    }

    private sealed class EncryptionState
    {
        public required EncryptionVersion DefaultVersion { get; init; }
        public required IReadOnlyList<EncryptionVersion> Versions { get; init; }

        public EncryptionVersion? FindVersion(string encryptedId) =>
            Versions
                .FirstOrDefault(version =>
                    encryptedId.StartsWith(
                        version.Prefix,
                        StringComparison.Ordinal));
    }
}
