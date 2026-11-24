using System.Text.Json;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.UnitTests.Moments.Data;

namespace HealthyFac.Seeder.UnitTests.Moments;

public sealed class RelativeMomentJsonConverterTests
{
    private static readonly JsonSerializerOptions
        Options = new() { Converters = { new RelativeMomentJsonConverter() } };

    [Theory]
    [MemberData(nameof(RelativeMomentTestData.InvalidTexts), MemberType = typeof(RelativeMomentTestData))]
    public void Read_WithAnInvalidText_FailsWithTheText(string text)
    {
        var act = () => JsonSerializer.Deserialize<RelativeMoment>(JsonSerializer.Serialize(text), Options);

        act.Should().Throw<JsonException>().WithMessage($"*'{text}'*");
    }

    [Theory]
    [MemberData(nameof(RelativeMomentTestData.ValidTexts), MemberType = typeof(RelativeMomentTestData))]
    public void ReadAndWrite_WithAValidText_GivesTheSameText(string text)
    {
        var moment = JsonSerializer.Deserialize<RelativeMoment>(JsonSerializer.Serialize(text), Options);

        var written = JsonSerializer.Serialize(moment, Options);

        written.Should().Be(JsonSerializer.Serialize(text));
    }

    [Fact]
    public void Read_WithANumber_Fails()
    {
        var act = () => JsonSerializer.Deserialize<RelativeMoment>(json: "5", Options);

        act.Should().Throw<JsonException>();
    }
}
