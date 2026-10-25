using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Shared.UnitTests.IdEncryption.Data;
using HealthyFac.Shared.Web.IdEncryption;

namespace HealthyFac.Shared.UnitTests.IdEncryption;

public sealed class IdEncryptionCollectionConventionTests
{
    private readonly IIdEncryptor _encryptor =
        new AesKwIdEncryptor(IdEncryptionCollectionConventionTestData.PrimaryKey);

    private readonly JsonSerializerOptions _jsonOptions;

    public IdEncryptionCollectionConventionTests()
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
        nameof(IdEncryptionCollectionConventionTestData.Models),
        MemberType = typeof(IdEncryptionCollectionConventionTestData)
    )]
    public void Serialize_CollectionsMatchingIdsConvention_WritesEncryptedElementsInOrder(
        CollectionConventionTestModel model
    )
    {
        var parsed = JsonNode.Parse(JsonSerializer.Serialize(model, _jsonOptions));

        parsed.Should().NotBeNull();
        DecryptElements(parsed[IdEncryptionCollectionConventionTestData.DepartmentIdsPropertyName])
            .Should()
            .Equal(model.DepartmentIds);
        DecryptElements(parsed[IdEncryptionCollectionConventionTestData.MachineIdsPropertyName])
            .Should()
            .Equal(model.MachineIds);
        DecryptElements(parsed[IdEncryptionCollectionConventionTestData.StepIdsPropertyName])
            .Should()
            .Equal(model.StepIds);
    }

    [Theory]
    [MemberData(
        nameof(IdEncryptionCollectionConventionTestData.Models),
        MemberType = typeof(IdEncryptionCollectionConventionTestData)
    )]
    public void Serialize_IgnoredOrNonConventionCollections_WritesPlainGuids(CollectionConventionTestModel model)
    {
        var parsed = JsonNode.Parse(JsonSerializer.Serialize(model, _jsonOptions));

        parsed.Should().NotBeNull();
        parsed[IdEncryptionCollectionConventionTestData.IgnoredIdsPropertyName]!.AsArray()
            .Select(element => element!.GetValue<Guid>())
            .Should()
            .Equal(model.IgnoredIds);
        parsed[IdEncryptionCollectionConventionTestData.CorrelationTokensPropertyName]!.AsArray()
            .Select(element => element!.GetValue<Guid>())
            .Should()
            .Equal(model.CorrelationTokens);
    }

    [Theory]
    [MemberData(
        nameof(IdEncryptionCollectionConventionTestData.Models),
        MemberType = typeof(IdEncryptionCollectionConventionTestData)
    )]
    public void Serialize_WithNullCollection_WritesNull(CollectionConventionTestModel model)
    {
        var parsed = JsonNode.Parse(JsonSerializer.Serialize(model, _jsonOptions));

        parsed.Should().NotBeNull();
        parsed.AsObject()
            .Should()
            .ContainKey(IdEncryptionCollectionConventionTestData.OptionalIdsPropertyName)
            .WhoseValue.Should()
            .BeNull();
    }

    [Theory]
    [MemberData(
        nameof(IdEncryptionCollectionConventionTestData.Models),
        MemberType = typeof(IdEncryptionCollectionConventionTestData)
    )]
    public void RoundTrip_WithCollectionModel_KeepsValuesAndOrder(CollectionConventionTestModel model)
    {
        var serialized = JsonSerializer.Serialize(model, _jsonOptions);

        var deserialized = JsonSerializer.Deserialize<CollectionConventionTestModel>(serialized, _jsonOptions);

        deserialized.Should().BeEquivalentTo(model, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Deserialize_WithNonArrayIdsValue_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<IdsOnlyTestModel>(
            IdEncryptionCollectionConventionTestData.NonArrayIdsJson,
            _jsonOptions);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_WithInvalidElement_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<IdsOnlyTestModel>(
            IdEncryptionCollectionConventionTestData.InvalidElementJson,
            _jsonOptions);

        act.Should().Throw<JsonException>();
    }

    private List<Guid> DecryptElements(JsonNode? array)
        => array!.AsArray()
            .Select(element => element!.GetValue<string>())
            .Select(_encryptor.Decrypt)
            .ToList();
}
