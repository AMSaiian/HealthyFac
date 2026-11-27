using System.Text.Json;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Infrastructure.Agents.Tools;
using HealthyFac.Insights.UnitTests.Agents.Data;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Testing.Features;
using PlannerHistoryArguments = HealthyFac.Insights.Application.Agents.Tools.Reads.GetMetricHistoryArguments;

namespace HealthyFac.Insights.UnitTests.Agents;

[Feature(TestFeatures.Cases)]
[Feature(TestFeatures.Chat)]
public sealed class UtcDateTimeJsonConverterTests
{
    private static readonly DateTime ExpectedInstant = new(
        year: 2026, month: 11, day: 26, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    private readonly JsonSerializerOptions _options = new AgentJson(new PrefixIdEncryptor()).Tools;

    [Theory]
    [MemberData(
        nameof(UtcDateTimeJsonConverterTestData.UtcInstants), MemberType = typeof(UtcDateTimeJsonConverterTestData))]
    public void Deserialize_WithTimeText_ReadsUtc(string caseName, string text)
    {
        var json = ArgumentsJson(text);

        var arguments = JsonSerializer.Deserialize<PlannerHistoryArguments>(json, _options);

        arguments!.From!.Value.Should().Be(ExpectedInstant, caseName);
        arguments.From.Value.Kind.Should().Be(DateTimeKind.Utc, caseName);
    }

    [Fact]
    public void Deserialize_WithTextThatIsNoTime_Throws()
    {
        var act = () => JsonSerializer.Deserialize<PlannerHistoryArguments>(
            ArgumentsJson(text: "yesterday"), _options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_WithoutPlannedFrom_KeepsNull()
    {
        var arguments = JsonSerializer.Deserialize<SearchWorkOrdersArguments>(json: "{}", _options);

        arguments!.PlannedFrom.Should().BeNull();
    }

    private static string ArgumentsJson(string text) =>
        $$"""{"metricKey":"temperature","from":"{{text}}","to":"{{text}}"}""";
}
