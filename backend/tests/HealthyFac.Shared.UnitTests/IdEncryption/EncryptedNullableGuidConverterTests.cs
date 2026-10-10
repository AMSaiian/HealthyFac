using System.Text.Json;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Shared.UnitTests.IdEncryption.Data;
using HealthyFac.Shared.Web.IdEncryption.Converters;

namespace HealthyFac.Shared.UnitTests.IdEncryption;

public sealed class EncryptedNullableGuidConverterTests
{
    private readonly IIdEncryptor _encryptor = new AesKwIdEncryptor(ConverterTestData.PrimaryKey);
    private readonly JsonSerializerOptions _options;

    public EncryptedNullableGuidConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new EncryptedNullableGuidConverter(_encryptor));
    }

    [Fact]
    public void Write_WhenNull_WritesJsonNull()
    {
        var json = JsonSerializer.Serialize<Guid?>(null, _options);

        json.Should().Be("null");
    }

    [Theory]
    [MemberData(nameof(ConverterTestData.NullableGuidsWithValue), MemberType = typeof(ConverterTestData))]
    public void Write_WhenHasValue_WritesEncryptedVersionPrefixedString(Guid? value)
    {
        var json = JsonSerializer.Serialize(value, _options);

        json.Should().StartWith($"\"{IdEncryptionConstants.CurrentVersionPrefix}");
        var unquoted = json.Trim('"');
        _encryptor.Decrypt(unquoted).Should().Be(value!.Value);
    }

    [Fact]
    public void Read_WhenJsonNull_ReturnsNull()
    {
        var json = "null";

        var result = JsonSerializer.Deserialize<Guid?>(json, _options);

        result.Should().BeNull();
    }

    [Fact]
    public void Read_WhenEmptyString_ReturnsNull()
    {
        var json = "\"\"";

        var result = JsonSerializer.Deserialize<Guid?>(json, _options);

        result.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(ConverterTestData.NullableGuidsWithValue), MemberType = typeof(ConverterTestData))]
    public void Read_WhenEncryptedString_ReturnsDecryptedGuid(Guid? original)
    {
        var encrypted = _encryptor.Encrypt(original!.Value);
        var json = $"\"{encrypted}\"";

        var result = JsonSerializer.Deserialize<Guid?>(json, _options);

        result.Should().Be(original);
    }
}
