using System.Net;
using System.Text.Json;
using HealthyFac.Core.ApiTests.Drafts.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Core.Messaging.Machines;

namespace HealthyFac.Core.ApiTests.Drafts;

[Collection(CoreApiCollection.Name)]
public sealed class ApplyMachineDraftApiTests(CoreApiFixture fixture)
{
    private const string TakenSerialNumber = "TAKEN-SERIAL";
    private const string UpdatedName = "Renamed by draft";

    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Apply_CreateDraft_CreatesTheMachineAndMovesTheChildren()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync(hasChildren: true);
        var claims = _fixture.CreateMachineClaims(
            DraftApiTestData.DraftWritePermission, DraftApiTestData.MachineWritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.ApplyPath(draft.EncryptedId),
            claims,
            DraftApiTestData.ApplyBody(DraftApiTestData.ProposedName, DraftApiTestData.ProposedSerialNumber));

        var machineId = await ReadMachineIdAsync(response);
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var metrics = await _fixture.LoadMetricDraftsAsync();
        var statuses = await _fixture.LoadStatusDraftsAsync();
        var created = await _fixture.ReadOutboxMessagesAsync<MachineCreated>();
        var applied = await _fixture.ReadOutboxMessagesAsync<MachineDraftApplied>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        machineDrafts.Should().ContainSingle().Which.State.Should().Be(DraftState.Applied);
        machineDrafts[index: 0].AppliedMachineId.Should().Be(machineId);
        metrics.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
        metrics[index: 0].TargetMachineDraftId.Should().BeNull();
        statuses.Should().ContainSingle().Which.TargetMachineId.Should().Be(machineId);
        created.Should().ContainSingle().Which.Name.Should().Be(DraftApiTestData.ProposedName);
        applied.Should().ContainSingle();
        applied[index: 0].FileId.Should().Be(DraftApiTestData.FileId);
        applied[index: 0].MachineId.Should().Be(machineId);
        applied[index: 0].QueuedAt.Should().Be(DraftApiTestData.QueuedAt);
    }

    [Fact]
    public async Task Apply_UpdateDraft_ChangesOnlyTheMachineValues()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "UPDATE-SERIAL"));
        var draft = await _fixture.ArrangeMachineDraftAsync(machineId);
        var rowVersion = await _fixture.LoadMachineRowVersionAsync(machineId);
        var claims = _fixture.CreateMachineClaims(
            DraftApiTestData.DraftWritePermission, DraftApiTestData.MachineWritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.ApplyPath(draft.EncryptedId),
            claims,
            DraftApiTestData.ApplyBody(UpdatedName, serialNumber: "UPDATE-SERIAL", rowVersion));

        var values = await _fixture.LoadMachineValuesAsync(machineId);
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();
        var renamed = await _fixture.ReadOutboxMessagesAsync<MachineRenamed>();
        var applied = await _fixture.ReadOutboxMessagesAsync<MachineDraftApplied>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        values.Name.Should().Be(UpdatedName);
        values.SerialNumber.Should().Be(expected: "UPDATE-SERIAL");
        machineDrafts.Should().ContainSingle().Which.State.Should().Be(DraftState.Applied);
        renamed.Should().ContainSingle().Which.Name.Should().Be(UpdatedName);
        applied.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_UpdateDraft_WithStaleRowVersion_Returns409()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "STALE-SERIAL"));
        var draft = await _fixture.ArrangeMachineDraftAsync(machineId);
        var rowVersion = await _fixture.LoadMachineRowVersionAsync(machineId);
        var claims = _fixture.CreateMachineClaims(
            DraftApiTestData.DraftWritePermission, DraftApiTestData.MachineWritePermission);
        var machinePath = $"{MachineApiTestData.MachinesPath}/{_fixture.IdEncryptor.Encrypt(machineId)}";
        await _fixture.SendMachineRequestAsync(
            HttpMethod.Put,
            machinePath,
            claims,
            DraftApiTestData.ApplyBody(name: "Changed by hand", serialNumber: "STALE-SERIAL"));

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.ApplyPath(draft.EncryptedId),
            claims,
            DraftApiTestData.ApplyBody(UpdatedName, serialNumber: "STALE-SERIAL", rowVersion));
        var machineDrafts = await _fixture.LoadMachineDraftsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        machineDrafts.Should().ContainSingle().Which.State.Should().Be(DraftState.Pending);
    }

    [Fact]
    public async Task Apply_CreateDraft_WithTakenSerialNumber_Returns409()
    {
        await _fixture.ArrangeMachineAsync(TakenSerialNumber);
        var draft = await _fixture.ArrangeMachineDraftAsync();
        var claims = _fixture.CreateMachineClaims(
            DraftApiTestData.DraftWritePermission, DraftApiTestData.MachineWritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.ApplyPath(draft.EncryptedId),
            claims,
            DraftApiTestData.ApplyBody(DraftApiTestData.ProposedName, TakenSerialNumber));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Apply_Twice_Returns409ForTheSecondCall()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync();
        var claims = _fixture.CreateMachineClaims(
            DraftApiTestData.DraftWritePermission, DraftApiTestData.MachineWritePermission);
        var body = DraftApiTestData.ApplyBody(DraftApiTestData.ProposedName, DraftApiTestData.ProposedSerialNumber);
        await _fixture.SendMachineRequestAsync(
            HttpMethod.Post, DraftApiTestData.ApplyPath(draft.EncryptedId), claims, body);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post, DraftApiTestData.ApplyPath(draft.EncryptedId), claims, body);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        content.Should().Contain(DraftApiTestData.DraftNotPendingKey);
    }

    [Fact]
    public async Task Apply_DraftOfAnotherCompany_Returns404()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync(companyId: Guid.CreateVersion7());
        var claims = _fixture.CreateMachineClaims(
            DraftApiTestData.DraftWritePermission, DraftApiTestData.MachineWritePermission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.ApplyPath(draft.EncryptedId),
            claims,
            DraftApiTestData.ApplyBody(DraftApiTestData.ProposedName, DraftApiTestData.ProposedSerialNumber));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(nameof(ApplyPermissionTestData.MissingOnePermission), MemberType = typeof(ApplyPermissionTestData))]
    public async Task Apply_WithOnlyOnePermission_Returns403(string permission)
    {
        var draft = await _fixture.ArrangeMachineDraftAsync();
        var claims = _fixture.CreateMachineClaims(permission);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            DraftApiTestData.ApplyPath(draft.EncryptedId),
            claims,
            DraftApiTestData.ApplyBody(DraftApiTestData.ProposedName, DraftApiTestData.ProposedSerialNumber));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> ReadMachineIdAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);

        return _fixture.IdEncryptor.Decrypt(document.RootElement.GetProperty(propertyName: "machineId").GetString()!);
    }
}
