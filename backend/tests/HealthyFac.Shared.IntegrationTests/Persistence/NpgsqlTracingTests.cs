using System.Diagnostics;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using HealthyFac.Shared.IntegrationTests.Persistence.Data;
using HealthyFac.Testing.Tracing;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Persistence;

public sealed class NpgsqlTracingTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Query_WithoutParentActivity_WritesNoSpan()
    {
        using var capture = new SpanCapture(NpgsqlTracingTestData.SourceName);
        var marker = NpgsqlTracingTestData.CreateMarker();

        await RunQueryAsync(marker);

        var spans = capture.Spans.Where(span => HasMarker(span, marker)).ToList();

        spans.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_WithParentActivity_WritesChildSpan()
    {
        using var capture = new SpanCapture(NpgsqlTracingTestData.SourceName);
        var marker = NpgsqlTracingTestData.CreateMarker();
        using var parent = new Activity(NpgsqlTracingTestData.ParentName).Start();

        await RunQueryAsync(marker);

        var spans = capture.Spans.Where(span => HasMarker(span, marker)).ToList();

        spans.Should().ContainSingle().Which.ParentId.Should().Be(parent.Id);
    }

    private static bool HasMarker(Activity span, string marker) =>
        span.GetTagItem(NpgsqlTracingTestData.QueryTextTag)?.ToString()?.Contains(marker) == true;

    private async Task RunQueryAsync(string marker)
    {
        await using var dbContext = new TracingDbContext(fixture.ConnectionString);

        await dbContext.Database.ExecuteSqlRawAsync(NpgsqlTracingTestData.CreateMarkerSql(marker));
    }
}
