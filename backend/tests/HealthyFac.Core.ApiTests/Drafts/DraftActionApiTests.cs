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
    public async Task Dismiss_CreateDraft_DismissesIt()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync();

        var response = await DismissAsync(draft.EncryptedId);
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        machineDrafts.Should().ContainSingle().Which.State.Should().Be(DraftState.Dismissed);
    }

    [Fact]
    public async Task Dismiss_UpdateDraft_DismissesOnlyThatDraft()
    {
        var machineId =
            _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "DISMISS-UPDATE"));
        var draft = await _fixture.ArrangeMachineDraftAsync(machineId);
        await _fixture.ArrangeMachineDraftAsync(machineId);

        var response = await DismissAsync(draft.EncryptedId);
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        machineDrafts.Single(stored => stored.Id == draft.Id).State.Should().Be(DraftState.Dismissed);
        machineDrafts.Count(stored => stored.State == DraftState.Pending).Should().Be(expected: 1);
    }

    [Fact]
    public async Task MarkApplied_Route_Returns404()
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.MarkAppliedPath,
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission),
            jsonBody: "{\"ids\":[]}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Dismiss_Twice_Returns422ForTheSecondCall()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync();
        await DismissAsync(draft.EncryptedId);

        var response = await DismissAsync(draft.EncryptedId);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        content.Should().Contain(DraftApiTestData.DraftNotPendingKey);
    }

    [Fact]
    public async Task Dismiss_DraftOfAnotherCompany_Returns404()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync(companyId: Guid.CreateVersion7());

        var response = await DismissAsync(draft.EncryptedId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(nameof(DraftApiTestData.DismissRoutesWithoutWrite), MemberType = typeof(DraftApiTestData))]
    public async Task Dismiss_WithoutWritePermission_Returns403(string caseName, string pathTemplate)
    {
        var draft = await _fixture.ArrangeMachineDraftAsync();
        var encryptedMachineId = await _fixture.ArrangeMachineAsync(serialNumber: "DISMISS-ALL-403");
        var claims = _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            pathTemplate.Replace(oldValue: "{draftId}", draft.EncryptedId)
                .Replace(oldValue: "{machineId}", encryptedMachineId),
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, caseName);
    }

    [Fact]
    public async Task DismissMachineDrafts_WithPendingDrafts_ReturnsTheCountAndDismissesThem()
    {
        var encryptedMachineId = await _fixture.ArrangeMachineAsync(serialNumber: "DISMISS-ALL");
        var machineId = _fixture.IdEncryptor.Decrypt(encryptedMachineId);
        await _fixture.ArrangeMachineDraftAsync(machineId);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.DismissMachineDraftsPath(encryptedMachineId),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission));
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain(expected: "\"dismissedCount\":1");
        machineDrafts.Should().ContainSingle().Which.State.Should().Be(DraftState.Dismissed);
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
}
