using System.Net;
using System.Net.Http.Headers;
using Azure.Storage.Blobs;
using Grpc.Core;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;
using HealthyFac.Document.Contracts.Files;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
public sealed class DocumentFilesRpcTests(DocumentApiFixture fixture) : IAsyncLifetime
{
    private const string ReadPermission = "files:read";

    private readonly DocumentApiFixture _fixture = fixture;
    private DocumentGrpcHost _host = null!;

    public ValueTask InitializeAsync()
    {
        _host = _fixture.CreateGrpcHost();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task GetFile_WithUserToken_ReturnsTheFile()
    {
        var file = await _fixture.ArrangeFileAsync();
        var token = _fixture.Tokens.CreateToken(_fixture.CreateClaims(ReadPermission));

        var reply = await _host.Client.GetFileAsync(
            new GetFileRequest { FileId = _fixture.IdEncryptor.Decrypt(file.EncryptedId).ToString() },
            DocumentGrpcHost.BearerHeaders(token),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.CompanyId.Should().Be(_fixture.CompanyId.ToString());
        reply.Purpose.Should().Be(expected: "attachment");
        reply.Extension.Should().Be(expected: "pdf");
        reply.HasMachineId.Should().BeFalse();
        reply.MachineChangedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFileLink_WithServiceTokenAndActorHeaders_ReturnsFileAndLink()
    {
        var file = await _fixture.ArrangeFileAsync();
        var token = _fixture.Tokens.CreateServiceToken(
            new TestServiceTokenClaims { ClientId = DocumentGrpcHost.AllowedClientId, Permissions = [ReadPermission] });

        var reply = await _host.Client.GetFileLinkAsync(
            new GetFileLinkRequest { FileId = _fixture.IdEncryptor.Decrypt(file.EncryptedId).ToString() },
            DocumentGrpcHost.BearerHeaders(token, Guid.CreateVersion7(), _fixture.CompanyId),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Url.Should().NotBeNullOrWhiteSpace();
        reply.ExpiresAt.ToDateTime().Should().BeAfter(DateTime.UtcNow);
        reply.File.CompanyId.Should().Be(_fixture.CompanyId.ToString());
    }

    [Fact]
    public async Task GetFileLink_ReturnsALinkToTheStorageEndpoint()
    {
        var file = await _fixture.ArrangeFileAsync();
        var token = _fixture.Tokens.CreateServiceToken(
            new TestServiceTokenClaims { ClientId = DocumentGrpcHost.AllowedClientId, Permissions = [ReadPermission] });
        var storageEndpoint = new BlobServiceClient(_fixture.StorageConnectionString).Uri.AbsoluteUri;

        var reply = await _host.Client.GetFileLinkAsync(
            new GetFileLinkRequest { FileId = _fixture.IdEncryptor.Decrypt(file.EncryptedId).ToString() },
            DocumentGrpcHost.BearerHeaders(token, Guid.CreateVersion7(), _fixture.CompanyId),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Url.Should().StartWith(storageEndpoint);
        reply.Url.Should().NotStartWith(DocumentApiTestData.PublicBlobBaseUrl);
    }

    [Fact]
    public async Task GetFile_WithTiedFile_ReturnsTheMachine()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync(machineId);
        var token = _fixture.Tokens.CreateToken(_fixture.CreateClaims(ReadPermission));

        var reply = await _host.Client.GetFileAsync(
            new GetFileRequest { FileId = _fixture.IdEncryptor.Decrypt(file.EncryptedId).ToString() },
            DocumentGrpcHost.BearerHeaders(token),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.MachineId.Should().Be(_fixture.IdEncryptor.Decrypt(machineId).ToString());
    }

    [Fact]
    public async Task GetFile_WithFileOfAnotherCompany_ReturnsNotFound()
    {
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(ReadPermission) with { Organizations = [Guid.CreateVersion7().ToString()] };
        var token = _fixture.Tokens.CreateToken(claims);

        var act = async () => await _host.Client.GetFileAsync(
            new GetFileRequest { FileId = _fixture.IdEncryptor.Decrypt(file.EncryptedId).ToString() },
            DocumentGrpcHost.BearerHeaders(token),
            cancellationToken: TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<RpcException>();

        thrown.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetFile_WithEmbeddedImage_ReturnsNotFound()
    {
        var imageId = await _fixture.ArrangeEmbeddedImageAsync();
        var token = _fixture.Tokens.CreateToken(_fixture.CreateClaims(ReadPermission));

        var act = async () => await _host.Client.GetFileAsync(
            new GetFileRequest { FileId = _fixture.IdEncryptor.Decrypt(imageId).ToString() },
            DocumentGrpcHost.BearerHeaders(token),
            cancellationToken: TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<RpcException>();

        thrown.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetFile_WithoutToken_ReturnsUnauthenticated()
    {
        var act = async () =>
            await _host.Client.GetFileAsync(
                new GetFileRequest { FileId = Guid.NewGuid().ToString() },
                cancellationToken: TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<RpcException>();

        thrown.Which.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }

    [Fact]
    public async Task GetFile_WithoutReadPermission_ReturnsPermissionDenied()
    {
        var token = _fixture.Tokens.CreateToken(_fixture.CreateClaims(TieFileTestData.WritePermission));

        var act = async () => await _host.Client.GetFileAsync(
            new GetFileRequest { FileId = Guid.NewGuid().ToString() },
            DocumentGrpcHost.BearerHeaders(token),
            cancellationToken: TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<RpcException>();

        thrown.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GrpcCall_OnTheApiPort_ReturnsNotFound()
    {
        using var client = _host.CreateApiClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post, requestUri: "/healthyfac.document.v1.DocumentFiles/GetFile")
        {
            Content = new ByteArrayContent([]),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType: "application/grpc");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
