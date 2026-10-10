using System.Buffers.Text;
using System.Security.Cryptography;
using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Shared.UnitTests.IdEncryption.Data;

namespace HealthyFac.Shared.UnitTests.IdEncryption;

public sealed class AesKwIdEncryptorTests
{
    [Theory]
    [MemberData(
        nameof(AesKwIdEncryptorTestData.SampleGuidsData),
        MemberType = typeof(AesKwIdEncryptorTestData)
    )]
    public void Encrypt_ThenDecrypt_ReturnsOriginalGuid(Guid originalId)
    {
        var encryptor = new AesKwIdEncryptor(AesKwIdEncryptorTestData.PrimaryKey);

        var encrypted = encryptor.Encrypt(originalId);
        var decrypted = encryptor.Decrypt(encrypted);

        decrypted.Should().Be(originalId);
    }

    [Theory]
    [MemberData(
        nameof(AesKwIdEncryptorTestData.TamperedByteIndicesData),
        MemberType = typeof(AesKwIdEncryptorTestData)
    )]
    public void Decrypt_WithOneBitModifiedCiphertext_ThrowsCryptographicException(int byteIndex)
    {
        var encryptor = new AesKwIdEncryptor(AesKwIdEncryptorTestData.PrimaryKey);
        var id = Guid.CreateVersion7();
        var encrypted = encryptor.Encrypt(id);

        var token = encrypted.Substring(AesKwIdEncryptorTestData.VersionPrefixLength);
        var rawBytes = Base64Url.DecodeFromChars(token);

        rawBytes[byteIndex] ^= 0x01;

        var tamperedEncrypted = $"{AesKwIdEncryptorTestData.VersionPrefix}{Base64Url.EncodeToString(rawBytes)}";

        var act = () => encryptor.Decrypt(tamperedEncrypted);

        act.Should().Throw<CryptographicException>();
    }

    [Theory]
    [MemberData(
        nameof(AesKwIdEncryptorTestData.InvalidBase64UrlPayloadsData),
        MemberType = typeof(AesKwIdEncryptorTestData)
    )]
    public void Decrypt_WithInvalidBase64Url_ThrowsFormatException(string invalidPayload)
    {
        var encryptor = new AesKwIdEncryptor(AesKwIdEncryptorTestData.PrimaryKey);

        var act = () => encryptor.Decrypt(invalidPayload);

        act.Should().Throw<FormatException>();
    }

    [Theory]
    [MemberData(
        nameof(AesKwIdEncryptorTestData.InvalidVersionPrefixedTokensData),
        MemberType = typeof(AesKwIdEncryptorTestData)
    )]
    public void Decrypt_WithMissingOrWrongVersionPrefix_ThrowsFormatException(string invalidPrefixedToken)
    {
        var encryptor = new AesKwIdEncryptor(AesKwIdEncryptorTestData.PrimaryKey);

        var act = () => encryptor.Decrypt(invalidPrefixedToken);

        act.Should().Throw<FormatException>();
    }

    [Theory]
    [MemberData(
        nameof(AesKwIdEncryptorTestData.InvalidKeyLengthsData),
        MemberType = typeof(AesKwIdEncryptorTestData)
    )]
    public void Constructor_WithInvalidKeyLength_ThrowsArgumentException(int keyLength)
    {
        var invalidKey = new byte[keyLength];

        var act = () => new AesKwIdEncryptor(invalidKey);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(
        nameof(AesKwIdEncryptorTestData.Rfc3394TestVectorsData),
        MemberType = typeof(AesKwIdEncryptorTestData)
    )]
    public void Encrypt_WithRfc3394TestVectors_ProducesExpectedCiphertext(
        byte[] key,
        Guid plaintextGuid,
        byte[] expectedCiphertext
    )
    {
        var encryptor = new AesKwIdEncryptor(key);

        var encryptedToken = encryptor.Encrypt(plaintextGuid);
        var rawCiphertext = Base64Url.DecodeFromChars(
            encryptedToken.Substring(AesKwIdEncryptorTestData.VersionPrefixLength)
        );

        rawCiphertext.Should().Equal(expectedCiphertext);
    }

    [Theory]
    [MemberData(
        nameof(AesKwIdEncryptorTestData.EmptyOrWhitespaceStringsData),
        MemberType = typeof(AesKwIdEncryptorTestData)
    )]
    public void Decrypt_WithEmptyOrWhitespaceString_ThrowsArgumentException(string emptyOrWhitespace)
    {
        var encryptor = new AesKwIdEncryptor(AesKwIdEncryptorTestData.PrimaryKey);

        var act = () => encryptor.Decrypt(emptyOrWhitespace);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [MemberData(
        nameof(AesKwIdEncryptorTestData.WrongLengthBase64UrlPayloadsData),
        MemberType = typeof(AesKwIdEncryptorTestData)
    )]
    public void Decrypt_WithPayloadWrongLength_ThrowsFormatException(string wrongLengthToken)
    {
        var encryptor = new AesKwIdEncryptor(AesKwIdEncryptorTestData.PrimaryKey);

        var act = () => encryptor.Decrypt(wrongLengthToken);

        act.Should().Throw<FormatException>();
    }
}
