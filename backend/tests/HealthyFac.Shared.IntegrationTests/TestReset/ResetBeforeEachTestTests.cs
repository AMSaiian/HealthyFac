using HealthyFac.Shared.IntegrationTests.TestReset.Data;

namespace HealthyFac.Shared.IntegrationTests.TestReset;

[Collection(ResetCollection.Name)]
public sealed class ResetBeforeEachTestTests(ResetFixture fixture)
{
    private readonly ResetFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(ResetTestData.RecordNames), MemberType = typeof(ResetTestData))]
    public async Task AddRecord_InTestOfSameCollection_FindsOnlyItsOwnRow(string name)
    {
        await _fixture.AddRecordAsync(name);

        var recordCount = await _fixture.CountRecordsAsync();

        recordCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task ResetAsync_WithRowsInEveryTable_KeepsReferenceDataAndRestoresBaseData()
    {
        await _fixture.AddRecordAsync(ResetTestData.MessageText);
        await _fixture.AddRecordAsync(ResetTestData.BaseItemName);

        await _fixture.ResetAsync();

        var recordCount = await _fixture.CountRecordsAsync();
        var referenceCount = await _fixture.CountReferenceItemsAsync();
        var baseCount = await _fixture.CountBaseItemsAsync();

        recordCount.Should().Be(expected: 0);
        referenceCount.Should().Be(ResetTestData.ReferenceItemCount);
        baseCount.Should().Be(expected: 1);
    }
}
