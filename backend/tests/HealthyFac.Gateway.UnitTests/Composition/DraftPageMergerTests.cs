using System.Text.Json;
using System.Text.Json.Nodes;
using HealthyFac.Gateway.UnitTests.Composition.Data;
using HealthyFac.Gateway.Web.Composition;
using HealthyFac.Gateway.Web.Composition.Drafts.Search;

namespace HealthyFac.Gateway.UnitTests.Composition;

public sealed class DraftPageMergerTests
{
    [Theory]
    [MemberData(nameof(DraftPageMergerTestData.Grids), MemberType = typeof(DraftPageMergerTestData))]
    public void Merge_ReadingPageByPage_ReturnsEveryRowOnceInTheSortedOrder(
        string caseName,
        string sortColumn,
        bool isDescending,
        DraftPagingSimulator core,
        DraftPagingSimulator telemetry,
        string[] expectedIds
    )
    {
        var ids = DraftPagingSimulator.ReadAll(
            core, telemetry, sortColumn, isDescending, DraftPageMergerTestData.PageLimit);

        ids.Should().Equal(expectedIds, caseName);
    }

    [Fact]
    public void Merge_WithEqualSortValues_PutsTheCoreRowBeforeTheTelemetryRow()
    {
        var core = new[] { Row(sortValueJson: "1", id: "c1") };
        var telemetry = new[] { Row(sortValueJson: "1", id: "t1") };

        var page = DraftPageMerger.Merge(
            core, telemetry, DraftSortComparers.State, isDescending: false, limit: 5, previous: null);

        page.Items.Select(item => item["id"]!.GetValue<string>()).Should().Equal("c1", "t1");
    }

    [Fact]
    public void Merge_WhenBothSourcesReturnFewerRowsThanTheLimit_HasNoNextPosition()
    {
        var core = new[] { Row(sortValueJson: "0", id: "c1") };
        var telemetry = new[] { Row(sortValueJson: "1", id: "t1") };

        var page = DraftPageMerger.Merge(
            core, telemetry, DraftSortComparers.State, isDescending: false, limit: 5, previous: null);

        page.NextPosition.Should().BeNull();
    }

    [Fact]
    public void Merge_WhenASourceIsFinished_KeepsItsPreviousPositionAndItsFlag()
    {
        var previousTelemetry = new KeysetPositionDto
            { SortValue = JsonDocument.Parse(json: "0").RootElement, Id = "t0" };

        var previous = new ComposedPosition { Telemetry = previousTelemetry };
        var core = new[] { Row(sortValueJson: "0", id: "c1"), Row(sortValueJson: "1", id: "c2") };

        var page = DraftPageMerger.Merge(
            core, [], DraftSortComparers.State, isDescending: false, limit: 2, previous);

        page.NextPosition.Should().NotBeNull();
        page.NextPosition.Telemetry.Should().Be(previousTelemetry);
        page.NextPosition.IsTelemetryFinished.Should().BeTrue();
        page.NextPosition.IsCoreFinished.Should().BeFalse();
    }

    [Fact]
    public void Merge_WithASortValueOfAnotherKind_ThrowsACompositionException()
    {
        var core = new[] { Row(sortValueJson: "\"pending\"", id: "c1") };

        var act = () => DraftPageMerger.Merge(
            core, [], DraftSortComparers.State, isDescending: false, limit: 5, previous: null);

        act.Should().Throw<CompositionException>().WithMessage(CompositionErrors.SortValueMismatch);
    }

    [Fact]
    public void Merge_WithARowWithoutAPosition_ThrowsACompositionException()
    {
        var core = new[] { new JsonObject { ["id"] = "c1" } };

        var act = () => DraftPageMerger.Merge(
            core, [], DraftSortComparers.State, isDescending: false, limit: 5, previous: null);

        act.Should().Throw<CompositionException>().WithMessage(CompositionErrors.RowPositionMissing);
    }

    private static JsonObject Row(string sortValueJson, string id) =>
        new()
        {
            ["id"] = id,
            ["position"] = new JsonObject
            {
                ["sortValue"] = JsonNode.Parse(sortValueJson),
                ["id"] = id,
            },
        };
}
