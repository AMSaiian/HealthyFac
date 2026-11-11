using System.Net;
using HealthyFac.Core.ApiTests.Drafts.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.Domain.Drafts;

namespace HealthyFac.Core.ApiTests.Drafts;

[Collection(CoreApiCollection.Name)]
public sealed class DraftActionApiTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Dismiss_CreateDraft_DismissesItsChildren()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync(hasChildren: true);

        var response = await DismissAsync(draft.EncryptedId);
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        machineDrafts.Should().ContainSingle().Which.State.Should().Be(DraftState.Dismissed);
        metrics.Should().ContainSingle().Which.State.Should().Be(DraftState.Dismissed);
        statuses.Should().ContainSingle().Which.State.Should().Be(DraftState.Dismissed);
    }

    [Fact]
    public async Task Dismiss_UpdateDraft_LeavesTheChildrenPending()
    {
        var machineId =
            _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "DISMISS-UPDATE"));
        var draft = await _fixture.ArrangeMachineDraftAsync(machineId);
        await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);

        var response = await DismissAsync(draft.EncryptedId);
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        machineDrafts.Should().ContainSingle().Which.State.Should().Be(DraftState.Dismissed);
        metrics.Should().ContainSingle().Which.State.Should().Be(DraftState.Pending);
    }

    [Fact]
    public async Task MarkApplied_ForChildOfPendingCreateDraft_Returns409ButDismissWorks()
    {
        var machineDraft = await _fixture.ArrangeMachineDraftAsync();
        var child = await _fixture.ArrangeMetricDraftAsync(targetMachineId: null, machineDraft.Id);

        var mark = await MarkAsync(child.EncryptedId);
        var markContent = await mark.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var dismiss = await DismissAsync(child.EncryptedId);

        mark.StatusCode.Should().Be(HttpStatusCode.Conflict);
        markContent.Should().Contain(DraftApiTestData.MachineDraftNotAppliedKey);
        dismiss.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkApplied_Twice_Returns204ForBothCalls()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "MARK-TWICE"));
        var child = await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);

        var first = await MarkAsync(child.EncryptedId);
        var second = await MarkAsync(child.EncryptedId);
        var metrics = await _fixture.LoadMetricDraftsAsync();

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
        metrics.Should().ContainSingle().Which.State.Should().Be(DraftState.Applied);
    }

    [Fact]
    public async Task MarkApplied_ForDismissedDraft_Returns409()
    {
        var machineId =
            _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "MARK-DISMISSED"));
        var child = await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        await DismissAsync(child.EncryptedId);

        var response = await MarkAsync(child.EncryptedId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task MarkApplied_WithOneUnmarkableDraft_ChangesNone()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "MARK-ATOMIC"));
        var machineDraft = await _fixture.ArrangeMachineDraftAsync();
        var markable = await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        var waiting = await _fixture.ArrangeMetricDraftAsync(targetMachineId: null, machineDraft.Id);

        var response = await MarkAsync(markable.EncryptedId, waiting.EncryptedId);
        var metrics = await _fixture.LoadMetricDraftsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        metrics.Should().OnlyContain(draft => draft.State == DraftState.Pending);
    }

    [Fact]
    public async Task MarkApplied_WithNullIds_Returns400()
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.MarkAppliedPath,
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission),
            DraftApiTestData.MarkBodyWithNullIds);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Dismiss_Twice_Returns409ForTheSecondCall()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync();
        await DismissAsync(draft.EncryptedId);

        var response = await DismissAsync(draft.EncryptedId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Dismiss_DraftOfAnotherCompany_Returns404()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync(companyId: Guid.CreateVersion7());

        var response = await DismissAsync(draft.EncryptedId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Dismiss_WithoutWritePermission_Returns403()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync();
        var claims = _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post, DraftApiTestData.DismissPath(draft.EncryptedId), claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DismissMachineDrafts_WithPendingDrafts_ReturnsTheCountAndDismissesThem()
    {
        var encryptedMachineId = await _fixture.ArrangeMachineAsync(serialNumber: "DISMISS-ALL");
        var machineId = _fixture.IdEncryptor.Decrypt(encryptedMachineId);
        await _fixture.ArrangeMachineDraftAsync(machineId);
        await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.DismissMachineDraftsPath(encryptedMachineId),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission));
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var metrics = await _fixture.LoadMetricDraftsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"dismissedCount\":2");
        metrics.Should().ContainSingle().Which.State.Should().Be(DraftState.Dismissed);
    }

    [Fact]
    public async Task DismissMachineDrafts_WithoutWritePermission_Returns403()
    {
        var encryptedMachineId = await _fixture.ArrangeMachineAsync(serialNumber: "DISMISS-ALL-403");

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.DismissMachineDraftsPath(encryptedMachineId),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DismissMachineDrafts_ForAMachineOutsideTheCompany_Returns404()
    {
        var encryptedMachineId = _fixture.IdEncryptor.Encrypt(Guid.CreateVersion7());

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.DismissMachineDraftsPath(encryptedMachineId),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> DismissAsync(string encryptedId) =>
        _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.DismissPath(encryptedId),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission));

    private Task<HttpResponseMessage> MarkAsync(params string[] encryptedIds) =>
        _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.MarkAppliedPath,
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission),
            DraftApiTestData.MarkBody(encryptedIds));
}
