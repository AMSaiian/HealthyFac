using Grpc.Core;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;
using HealthyFac.Document.Contracts.Files;
using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Features;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
[Feature(TestFeatures.Chat)]
[Feature(TestFeatures.Files)]
public sealed class DocumentRpcTests(DocumentApiFixture fixture) : IAsyncLifetime
{
    private const string ReadPermission = "files:read";
    private const string OtherPermission = "machines:read";
    private const string NamePrefix = "man";
    private const string MissingNamePrefix = "zzz";

    private readonly DocumentApiFixture _fixture = fixture;
    private DocumentGrpcHost _host = null!;

    public ValueTask InitializeAsync()
    {
        _host = _fixture.CreateGrpcHost();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task SearchFiles_WithTheStartOfAName_ReturnsTheSameItemsAsTheHttpLookup()
    {
        var file = await _fixture.ArrangeFileAsync();

        var reply = await _host.Client.SearchFilesAsync(
            new SearchFilesRequest { Search = NamePrefix },
            ServiceHeaders([ReadPermission]),
            cancellationToken: TestContext.Current.CancellationToken);

        var found = reply.Files.Should().ContainSingle().Subject;
        found.FileId.Should().Be(_fixture.IdEncryptor.Decrypt(file.EncryptedId).ToString());
        found.FileName.Should().Be(expected: "manual");
        found.Extension.Should().Be(expected: "pdf");
        found.HasMachineId.Should().BeFalse();
    }

    [Fact]
    public async Task SearchFiles_WithATextThatNoNameStartsWith_ReturnsNoFile()
    {
        await _fixture.ArrangeFileAsync();

        var reply = await _host.Client.SearchFilesAsync(
            new SearchFilesRequest { Search = MissingNamePrefix },
            ServiceHeaders([ReadPermission]),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchFiles_WithAnotherCompany_ReturnsNoFile()
    {
        await _fixture.ArrangeFileAsync();

        var reply = await _host.Client.SearchFilesAsync(
            new SearchFilesRequest { Search = NamePrefix },
            ServiceHeaders([ReadPermission], Guid.CreateVersion7()),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchFiles_WithoutTheReadPermission_ReturnsPermissionDenied()
    {
        await _fixture.ArrangeFileAsync();

        var act = async () => await _host.Client.SearchFilesAsync(
            new SearchFilesRequest { Search = NamePrefix },
            ServiceHeaders([OtherPermission]),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    private Metadata ServiceHeaders(string[] permissions, Guid? companyId = null)
    {
        var token = _fixture.Tokens.CreateServiceToken(
            new TestServiceTokenClaims { ClientId = DocumentGrpcHost.AllowedClientId, Permissions = permissions });

        return DocumentGrpcHost.BearerHeaders(token, Guid.CreateVersion7(), companyId ?? _fixture.CompanyId);
    }
}
