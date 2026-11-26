using System.Text.Json.Nodes;
using HealthyFac.Gateway.UnitTests.Composition.Data;
using HealthyFac.Gateway.Web.Composition.Drafts.Search;
using HealthyFac.Testing.Features;

namespace HealthyFac.Gateway.UnitTests.Composition;

[Feature(TestFeatures.Gateway)]
[Feature(TestFeatures.Drafts)]
public sealed class DraftFilterSplitterTests
{
    [Theory]
    [MemberData(nameof(DraftFilterSplitterTestData.Splits), MemberType = typeof(DraftFilterSplitterTestData))]
    public void Split_OfTheFilter_GivesEachSourceItsFilterOrSkipsIt(
        string caseName,
        string? filterJson,
        bool isCoreSkipped,
        string? expectedCoreJson,
        bool isTelemetrySkipped,
        string? expectedTelemetryJson
    )
    {
        var filter = filterJson is null ? null : JsonNode.Parse(filterJson)!.AsObject();

        var split = DraftFilterSplitter.Split(filter);

        split.Core.IsSkipped.Should().Be(isCoreSkipped, caseName);
        split.Telemetry.IsSkipped.Should().Be(isTelemetrySkipped, caseName);

        JsonNode.DeepEquals(split.Core.Filter, expectedCoreJson is null ? null : JsonNode.Parse(expectedCoreJson))
            .Should()
            .BeTrue(caseName);

        JsonNode.DeepEquals(
                split.Telemetry.Filter,
                expectedTelemetryJson is null ? null : JsonNode.Parse(expectedTelemetryJson))
            .Should()
            .BeTrue(caseName);
    }

    [Fact]
    public void Split_OfAFilterObject_LeavesTheInputUnchanged()
    {
        var filter = JsonNode.Parse(json: """{"kinds":{"values":["machine","metric"]}}""")!.AsObject();

        _ = DraftFilterSplitter.Split(filter);

        filter.ToJsonString().Should().Be(expected: """{"kinds":{"values":["machine","metric"]}}""");
    }
}
