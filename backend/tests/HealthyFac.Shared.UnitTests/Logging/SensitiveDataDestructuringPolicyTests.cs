using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.UnitTests.Logging.Data;
using HealthyFac.Shared.Web.Logging;
using Serilog;
using Serilog.Events;

namespace HealthyFac.Shared.UnitTests.Logging;

public sealed class SensitiveDataDestructuringPolicyTests
{
    [Theory]
    [MemberData(
        nameof(DestructuringTestData.SensitivePropertyData),
        MemberType = typeof(DestructuringTestData)
    )]
    public void Destructure_WithSensitiveProperty_MasksSensitivePropertyAndLeavesPlainPropertyIntact(
        SensitivePropertyUserDto dto,
        string expectedUsername,
        string expectedMaskedPassword
    )
    {
        var sink = new TestLogEventSink();
        CreateLogger(sink).Information("User: {@Payload}", dto);

        sink.Events.Should().ContainSingle();
        var property = sink.Events[0].Properties["Payload"].Should().BeOfType<StructureValue>().Subject;

        property.Properties.Single(p => p.Name == nameof(SensitivePropertyUserDto.Username))
            .Value.Should()
            .BeOfType<ScalarValue>()
            .Which.Value.Should()
            .Be(expectedUsername);

        property.Properties.Single(p => p.Name == nameof(SensitivePropertyUserDto.Password))
            .Value.Should()
            .BeOfType<ScalarValue>()
            .Which.Value.Should()
            .Be(expectedMaskedPassword);
    }

    [Theory]
    [MemberData(
        nameof(DestructuringTestData.FullyMaskedData),
        MemberType = typeof(DestructuringTestData)
    )]
    public void Destructure_WithFullyMaskedClass_ReturnsScalarMask(FullyMaskedPayloadDto dto)
    {
        var sink = new TestLogEventSink();
        CreateLogger(sink).Information("Payload: {@Payload}", dto);

        sink.Events.Should().ContainSingle();
        sink.Events[0]
            .Properties["Payload"]
            .Should()
            .BeOfType<ScalarValue>()
            .Which.Value.Should()
            .Be(MaskingConstants.DefaultMask);
    }

    [Fact]
    public void Destructure_WithNestedSensitiveObject_MasksNestedPropertyRecursively()
    {
        var sink = new TestLogEventSink();
        CreateLogger(sink)
            .Information(
                "Payload: {@Payload}",
                new NestedSensitiveDto
                {
                    Title = "Top Level",
                    User = new SensitivePropertyUserDto
                    {
                        Username = "nested_user",
                        Password = "NestedPassword789!",
                    },
                }
            );

        sink.Events.Should().ContainSingle();
        var root = sink.Events[0].Properties["Payload"].Should().BeOfType<StructureValue>().Subject;

        var userStructure = root.Properties.Single(p => p.Name == nameof(NestedSensitiveDto.User))
            .Value.Should()
            .BeOfType<StructureValue>()
            .Subject;

        userStructure.Properties.Single(p => p.Name == nameof(SensitivePropertyUserDto.Password))
            .Value.Should()
            .BeOfType<ScalarValue>()
            .Which.Value.Should()
            .Be(MaskingConstants.DefaultMask);
    }

    [Fact]
    public void Destructure_WithNullSensitiveProperty_LeavesValueNull()
    {
        var sink = new TestLogEventSink();
        CreateLogger(sink).Information("Payload: {@Payload}", new NullableSensitiveDto { OptionalSecret = null });

        sink.Events.Should().ContainSingle();
        var property = sink.Events[0].Properties["Payload"].Should().BeOfType<StructureValue>().Subject;

        property.Properties.Single(p => p.Name == nameof(NullableSensitiveDto.OptionalSecret))
            .Value.Should()
            .BeOfType<ScalarValue>()
            .Which.Value.Should()
            .BeNull();
    }

    [Theory]
    [MemberData(
        nameof(DestructuringTestData.SensitiveCollectionData),
        MemberType = typeof(DestructuringTestData)
    )]
    public void Destructure_WithSensitiveCollection_MasksAllCollectionItems(SensitiveCollectionDto dto)
    {
        var sink = new TestLogEventSink();
        CreateLogger(sink).Information("Payload: {@Payload}", dto);

        sink.Events.Should().ContainSingle();
        var root = sink.Events[0].Properties["Payload"].Should().BeOfType<StructureValue>().Subject;

        var seqVal = root.Properties.Single(p => p.Name == nameof(SensitiveCollectionDto.SecretTokens))
            .Value.Should()
            .BeOfType<SequenceValue>()
            .Subject;

        seqVal.Elements.Should().HaveCount(dto.SecretTokens.Count);
        foreach (var item in seqVal.Elements)
        {
            item.Should().BeOfType<ScalarValue>().Which.Value.Should().Be(MaskingConstants.DefaultMask);
        }
    }

    [Theory]
    [MemberData(
        nameof(DestructuringTestData.SensitiveDictionaryData),
        MemberType = typeof(DestructuringTestData)
    )]
    public void Destructure_WithSensitiveDictionary_MasksDictionaryValues(SensitiveDictionaryDto dto)
    {
        var sink = new TestLogEventSink();
        CreateLogger(sink).Information("Payload: {@Payload}", dto);

        sink.Events.Should().ContainSingle();
        var root = sink.Events[0].Properties["Payload"].Should().BeOfType<StructureValue>().Subject;

        var dictVal = root.Properties.Single(p => p.Name == nameof(SensitiveDictionaryDto.Headers))
            .Value.Should()
            .BeOfType<DictionaryValue>()
            .Subject;

        dictVal.Elements.Should().HaveCount(dto.Headers.Count);
        foreach (var kvp in dictVal.Elements)
        {
            kvp.Value.Should().BeOfType<ScalarValue>().Which.Value.Should().Be(MaskingConstants.DefaultMask);
        }
    }

    [Theory]
    [MemberData(
        nameof(DestructuringTestData.PlainUserData),
        MemberType = typeof(DestructuringTestData)
    )]
    public void Destructure_WithPlainObject_DoesNotMaskProperties(PlainUserDto dto)
    {
        var sink = new TestLogEventSink();
        CreateLogger(sink).Information("Payload: {@Payload}", dto);

        sink.Events.Should().ContainSingle();
        var root = sink.Events[0].Properties["Payload"].Should().BeOfType<StructureValue>().Subject;

        root.Properties.Single(p => p.Name == nameof(PlainUserDto.Name))
            .Value.Should()
            .BeOfType<ScalarValue>()
            .Which.Value.Should()
            .Be(dto.Name);

        root.Properties.Single(p => p.Name == nameof(PlainUserDto.Age))
            .Value.Should()
            .BeOfType<ScalarValue>()
            .Which.Value.Should()
            .Be(dto.Age);
    }

    [Theory]
    [MemberData(
        nameof(DestructuringTestData.SimpleScalarData),
        MemberType = typeof(DestructuringTestData)
    )]
    public void Destructure_WithSimpleScalar_PassesThroughAsScalar(object scalar)
    {
        var sink = new TestLogEventSink();
        CreateLogger(sink).Information("Payload: {@Payload}", scalar);

        sink.Events.Should().ContainSingle();
        sink.Events[0]
            .Properties["Payload"]
            .Should()
            .BeOfType<ScalarValue>()
            .Which.Value.Should()
            .Be(scalar);
    }

    private static ILogger CreateLogger(TestLogEventSink sink) =>
        new LoggerConfiguration()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Sink(sink)
            .CreateLogger();
}
