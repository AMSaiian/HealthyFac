using System.Text.Json;
using HealthyFac.Shared.UnitTests.Enumerations.Data;
using HealthyFac.Shared.UnitTests.Serialization.Data;
using HealthyFac.Shared.Web.Serialization;

namespace HealthyFac.Shared.UnitTests.Serialization;

public sealed class EnumerationJsonConverterTests
{
    private readonly JsonSerializerOptions _options = CreateOptions();

    [Theory]
    [MemberData(
        nameof(EnumerationJsonTestData.MembersWithJson),
        MemberType = typeof(EnumerationJsonTestData)
    )]
    public void Serialize_WithEnumerationMember_WritesItsValue(
        MachineStateTestEnumeration member,
        string expectedJson
    )
    {
        JsonSerializer.Serialize(new EnumerationPayload { Status = member }, _options)
            .Should()
            .Be(expectedJson);
    }

    [Theory]
    [MemberData(
        nameof(EnumerationJsonTestData.MembersWithJson),
        MemberType = typeof(EnumerationJsonTestData)
    )]
    public void Deserialize_WithKnownValue_ResolvesDeclaredMember(
        MachineStateTestEnumeration member,
        string json
    )
    {
        JsonSerializer.Deserialize<EnumerationPayload>(json, _options)!
            .Status
            .Should()
            .BeSameAs(member);
    }

    [Fact]
    public void Deserialize_WithUnknownValue_ThrowsJsonException()
    {
        var act = () => JsonSerializer.Deserialize<EnumerationPayload>(
            EnumerationJsonTestData.UnknownValueJson,
            _options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_WithNullOptionalValue_LeavesMemberNull()
    {
        JsonSerializer.Deserialize<EnumerationPayload>(
                """{"status":"operational","optional":null}""",
                _options)!
            .Optional
            .Should()
            .BeNull();
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new EnumerationJsonConverter());

        return options;
    }
}
