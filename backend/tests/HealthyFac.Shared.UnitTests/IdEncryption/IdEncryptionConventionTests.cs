using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Shared.UnitTests.IdEncryption.Data;
using HealthyFac.Shared.Web.IdEncryption;

namespace HealthyFac.Shared.UnitTests.IdEncryption;

public sealed class IdEncryptionConventionTests
{
    private readonly IIdEncryptor _encryptor = new AesKwIdEncryptor(IdEncryptionConventionTestData.PrimaryKey);
    private readonly JsonSerializerOptions _jsonOptions;

    public IdEncryptionConventionTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    typeInfo => IdEncryptionExtensions.ApplyIdEncryption(typeInfo, _encryptor),
                },
            },
        };
    }

    [Theory]
    [MemberData(
        nameof(IdEncryptionConventionTestData.ModelsWithTenantId),
        MemberType = typeof(IdEncryptionConventionTestData)
    )]
    public void Serialize_PropertiesMatchingIdConvention_AreEncrypted(ConventionTestModel model)
    {
        var json = JsonSerializer.Serialize(model, _jsonOptions);
        var parsed = JsonNode.Parse(json);

        parsed.Should().NotBeNull();

        var idString = parsed[IdEncryptionConventionTestData.IdPropertyName]?.GetValue<string>();
        var machineIdString = parsed[IdEncryptionConventionTestData.MachineIdPropertyName]?.GetValue<string>();
        var tenantIdString = parsed[IdEncryptionConventionTestData.TenantIdPropertyName]?.GetValue<string>();
        var ignoredIdString = parsed[IdEncryptionConventionTestData.IgnoredIdPropertyName]?.GetValue<string>();
        var correlationTokenString = parsed[IdEncryptionConventionTestData.CorrelationTokenPropertyName]
            ?.GetValue<string>();
        var stepIdValue = parsed[IdEncryptionConventionTestData.StepIdPropertyName]?.GetValue<int>();

        idString.Should().StartWith(IdEncryptionConstants.CurrentVersionPrefix);
        machineIdString.Should().StartWith(IdEncryptionConstants.CurrentVersionPrefix);
        tenantIdString.Should().StartWith(IdEncryptionConstants.CurrentVersionPrefix);

        _encryptor.Decrypt(idString).Should().Be(model.Id);
        _encryptor.Decrypt(machineIdString).Should().Be(model.MachineId);
        _encryptor.Decrypt(tenantIdString).Should().Be(model.TenantId!.Value);

        ignoredIdString.Should().Be(model.IgnoredId.ToString());
        correlationTokenString.Should().Be(model.CorrelationToken.ToString());
        stepIdValue.Should().Be(model.StepId);
    }

    [Theory]
    [MemberData(
        nameof(IdEncryptionConventionTestData.ModelsWithTenantId),
        MemberType = typeof(IdEncryptionConventionTestData)
    )]
    public void RoundTrip_WithConventionModel_MaintainsExactGuidValues(ConventionTestModel model)
    {
        var serialized = JsonSerializer.Serialize(model, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConventionTestModel>(serialized, _jsonOptions);

        deserialized.Should().BeEquivalentTo(model);
    }
}
