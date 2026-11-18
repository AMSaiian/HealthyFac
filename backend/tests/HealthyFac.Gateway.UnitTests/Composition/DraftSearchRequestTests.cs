using System.Text.Json.Nodes;
using HealthyFac.Gateway.UnitTests.Composition.Data;
using HealthyFac.Gateway.Web.Composition.Drafts;

namespace HealthyFac.Gateway.UnitTests.Composition;

public sealed class DraftSearchRequestTests
{
    [Theory]
    [MemberData(nameof(DraftSearchRequestTestData.InvalidBodies), MemberType = typeof(DraftSearchRequestTestData))]
    public void TryRead_WithAnInvalidField_ReturnsNoRequestAndNamesTheField(
        string caseName,
        string bodyJson,
        string expectedField
    )
    {
        var request = DraftSearchRequest.TryRead(JsonNode.Parse(bodyJson)!.AsObject(), out var errors);

        request.Should().BeNull(caseName);
        errors.Keys.Should().Equal(expectedField);
    }

    [Fact]
    public void TryRead_WithoutASort_UsesThePendingFirstDefault()
    {
        var request = DraftSearchRequest.TryRead(JsonNode.Parse(json: """{"limit":5}""")!.AsObject(), out _);

        request.Should().NotBeNull();
        request!.SortColumn.Should().Be(DraftSortComparers.State);
        request.IsDescending.Should().BeFalse();
    }

    [Fact]
    public void TryRead_WithADescendingSortAndAnAfterPosition_ReadsBoth()
    {
        const string body = """
                            {"limit":5,"sort":{"column":"proposedAt","direction":"desc"},
                             "after":{"core":{"sortValue":"2026-01-01T00:00:00Z","id":"c1"},"isCoreFinished":true}}
                            """;

        var request = DraftSearchRequest.TryRead(JsonNode.Parse(body)!.AsObject(), out _);

        request!.IsDescending.Should().BeTrue();
        request.After!.Core!.Id.Should().Be(expected: "c1");
        request.After.IsCoreFinished.Should().BeTrue();
    }
}
