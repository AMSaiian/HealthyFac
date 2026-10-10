using System.Text.Json;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Shared.UnitTests.IdEncryption.Data;
using HealthyFac.Shared.Web.IdEncryption.Converters;

namespace HealthyFac.Shared.UnitTests.IdEncryption;

public sealed class EncryptedGuidConverterTests
{
    private readonly IIdEncryptor _encryptor = new AesKwIdEncryptor(ConverterTestData.PrimaryKey);
    private readonly JsonSerializerOptions _options;

    public EncryptedGuidConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new EncryptedGuidConverter(_encryptor));
    }

    [Theory]
    [MemberData(nameof(ConverterTestData.ValidGuids), MemberType = typeof(ConverterTestData))]
    public void Write_ValidGuid_WritesEncryptedVersionPrefixedString(Guid value)
    {
        var json = JsonSerializer.Serialize(value, _options);

        json.Should().StartWith($"\"{IdEncryptionConstants.CurrentVersionPrefix}");
        var unquoted = json.Trim('"');
        _encryptor.Decrypt(unquoted).Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(ConverterTestData.ValidGuids), MemberType = typeof(ConverterTestData))]
    public void Read_ValidEncryptedString_ReturnsDecryptedGuid(Guid original)
    {
        var encrypted = _encryptor.Encrypt(original);
        var json = $"\"{encrypted}\"";

        var result = JsonSerializer.Deserialize<Guid>(json, _options);

        result.Should().Be(original);
    }

    [Fact]
    public void Read_EmptyString_ReturnsGuidEmpty()
    {
        var json = "\"\"";

        var result = JsonSerializer.Deserialize<Guid>(json, _options);

        result.Should().Be(Guid.Empty);
    }

    [Theory]
    [MemberData(nameof(ConverterTestData.MalformedTokens), MemberType = typeof(ConverterTestData))]
    public void Read_WithMalformedToken_ThrowsJsonException(string token)
    {
        var act = () => JsonSerializer.Deserialize<Guid>($"\"{token}\"", _options);

        act.Should().Throw<JsonException>();
    }
}
