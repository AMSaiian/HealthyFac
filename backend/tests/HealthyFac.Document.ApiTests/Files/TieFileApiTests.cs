using System.Net;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
public sealed class TieFileApiTests(DocumentApiFixture fixture)
{
    private readonly DocumentApiFixture _fixture = fixture;

    [Fact]
    public async Task TieFile_WithMachineOfTheCompany_Returns204AndShowsInTheMachineSearch()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(TieFileTestData.WritePermission, TieFileTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.TieBody(machineId, file.RowVersion));
        var search = await _fixture.SendAsync(
            HttpMethod.Post,
            $"{UploadFileApiTestData.MachineFilesPath(machineId)}/search",
            claims,
            TieFileTestData.SearchBody);
        var searchBody = await search.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        searchBody.Should().Contain(file.EncryptedId);
    }

    [Fact]
    public async Task UntieFile_WithTiedFile_Returns204AndLeavesTheMachineSearch()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(TieFileTestData.WritePermission, TieFileTestData.ReadPermission);
        await _fixture.SendAsync(
            HttpMethod.Put,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.TieBody(machineId, file.RowVersion));
        var rowVersion = await _fixture.LoadRowVersionAsync(file.EncryptedId);

        var response = await _fixture.SendAsync(
            HttpMethod.Delete,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.UntieBody(rowVersion));
        var search = await _fixture.SendAsync(
            HttpMethod.Post,
            $"{UploadFileApiTestData.MachineFilesPath(machineId)}/search",
            claims,
            TieFileTestData.SearchBody);
        var searchBody = await search.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        searchBody.Should().NotContain(file.EncryptedId);
    }

    [Fact]
    public async Task TieFile_WithStaleRowVersion_Returns409()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(TieFileTestData.WritePermission);
        await _fixture.SendAsync(
            HttpMethod.Put,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.TieBody(machineId, file.RowVersion));

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.TieBody(machineId, file.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UntieFile_WithStaleRowVersion_Returns409()
    {
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(TieFileTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Delete,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.UntieBody(file.RowVersion + 1));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TieFile_WithMachineOfAnotherCompany_Returns404()
    {
        var machineId = await _fixture.ArrangeMachineAsync(Guid.CreateVersion7());
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(TieFileTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.TieBody(machineId, file.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TieFile_WithDeletedMachine_Returns404()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync();
        await _fixture.DeleteMachineRegistrationAsync(machineId);
        var claims = _fixture.CreateClaims(TieFileTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.TieBody(machineId, file.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TieFile_WithEmbeddedImage_Returns404()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var imageId = await _fixture.ArrangeEmbeddedImageAsync();
        var claims = _fixture.CreateClaims(TieFileTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            TieFileTestData.MachineOfFilePath(imageId),
            claims,
            TieFileTestData.TieBody(machineId, rowVersion: 1));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TieFile_WithoutWritePermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(TieFileTestData.ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.TieBody(machineId, file.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TieAndUntie_WithFile_StoreOneMessageForEachChange()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(TieFileTestData.WritePermission);
        await _fixture.SendAsync(
            HttpMethod.Put,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.TieBody(machineId, file.RowVersion));
        var rowVersion = await _fixture.LoadRowVersionAsync(file.EncryptedId);
        await _fixture.SendAsync(
            HttpMethod.Delete,
            TieFileTestData.MachineOfFilePath(file.EncryptedId),
            claims,
            TieFileTestData.UntieBody(rowVersion));

        var messages = await _fixture.ReadFileMachineChangedMessagesAsync();

        messages.Should().HaveCount(expected: 2);
        messages[index: 0].MachineId.Should().Be(_fixture.IdEncryptor.Decrypt(machineId));
        messages[index: 1].MachineId.Should().BeNull();
        messages[index: 1].ChangedAt.Should().BeOnOrAfter(messages[index: 0].ChangedAt);
    }
}
