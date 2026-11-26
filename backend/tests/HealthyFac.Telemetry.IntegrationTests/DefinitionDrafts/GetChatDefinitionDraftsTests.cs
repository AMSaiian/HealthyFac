using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
[Feature(TestFeatures.Chat)]
public sealed class GetChatDefinitionDraftsTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetChatDrafts_WithDraftsOfTwoProposalsOfOneChat_ListsThemAndNotTheIngestDrafts()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);

        await _fixture.StoreProposedDefinitionDraftsAsync(
            companyId, ProposalBuilder.ChatMessage(companyId, machineId));

        await _fixture.StoreProposedDefinitionDraftsAsync(
            companyId,
            ProposalBuilder.ChatMessage(companyId, machineId) with { ChatMessageId = Guid.CreateVersion7() });

        var response = await _fixture.GetChatDefinitionDraftsAsync(companyId, DefinitionDraftTestData.ChatId);

        response.Metrics.Should().HaveCount(expected: 2);
        response.Metrics.Should().OnlyContain(draft => draft.Proposal.Source == DraftSource.Chat);
        response.Metrics.Should().OnlyContain(draft => draft.Proposal.ChatId == DefinitionDraftTestData.ChatId);
        response.Metrics.Should().OnlyContain(draft => draft.Proposal.RequestedByName == "Ann Owner");
        response.Statuses.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task GetChatDrafts_ForAnotherCompany_ReturnsNothing()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);

        await _fixture.StoreProposedDefinitionDraftsAsync(
            companyId, ProposalBuilder.ChatMessage(companyId, machineId));

        var response = await _fixture.GetChatDefinitionDraftsAsync(
            Guid.CreateVersion7(), DefinitionDraftTestData.ChatId);

        response.Metrics.Should().BeEmpty();
        response.Statuses.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchChatDrafts_ListsOnlyTheDraftsOfTheChatOfTheCompany()
    {
        var companyId = DefinitionDraftTestData.CompanyId;
        var machineId = await _fixture.RegisterTenantMachineAsync(companyId);
        await _fixture.StoreMetricDraftOnMachineAsync(companyId, machineId, isApplied: false);

        await _fixture.StoreProposedDefinitionDraftsAsync(
            companyId, ProposalBuilder.ChatMessage(companyId, machineId));

        await _fixture.StoreProposedDefinitionDraftsAsync(
            companyId,
            ProposalBuilder.ChatMessage(companyId, machineId) with
            {
                ChatId = Guid.CreateVersion7(),
                ChatMessageId = Guid.CreateVersion7(),
            });

        var ownIds = await _fixture.ReadChatDefinitionDraftsAsync(companyId, DefinitionDraftTestData.ChatId);

        var foreignIds = await _fixture.ReadChatDefinitionDraftsAsync(
            Guid.CreateVersion7(), DefinitionDraftTestData.ChatId);

        ownIds.Should().HaveCount(expected: 2);
        foreignIds.Should().BeEmpty();
    }
}
