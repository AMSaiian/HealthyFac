using Grpc.Core;
using HealthyFac.Insights.Infrastructure.Files;
using HealthyFac.Insights.UnitTests.Files.Data;
using HealthyFac.Shared.Application.DownstreamScopes;

namespace HealthyFac.Insights.UnitTests.Files;

public sealed class DocumentGrpcFileCatalogTests
{
    private const string WorkerScope = "files";

    private readonly DownstreamScopeHolder _holder = new();
    private readonly FakeDocumentCallInvoker _invoker;
    private readonly DocumentGrpcFileCatalog _catalog;

    public DocumentGrpcFileCatalogTests()
    {
        _invoker = new FakeDocumentCallInvoker(_holder);
        _catalog = new DocumentGrpcFileCatalog(new FakeGrpcClientFactory(_invoker), _holder);
    }

    [Fact]
    public async Task GetFileForUser_WithTiedFile_MapsEveryField()
    {
        _invoker.Respond = _ => DocumentCatalogTestData.CreateInfo(DocumentCatalogTestData.MachineId);

        var file = await _catalog.GetFileForUserAsync(
            DocumentCatalogTestData.FileId, TestContext.Current.CancellationToken);

        file.Should().NotBeNull();
        file!.FileId.Should().Be(DocumentCatalogTestData.FileId);
        file.CompanyId.Should().Be(DocumentCatalogTestData.CompanyId);
        file.MachineId.Should().Be(DocumentCatalogTestData.MachineId);
        file.MachineChangedAt.Should().Be(DocumentCatalogTestData.ChangedAt);
        file.Purpose.Should().Be(expected: "attachment");
        file.Extension.Should().Be(expected: "pdf");
        file.SizeBytes.Should().Be(expected: 2048);
    }

    [Fact]
    public async Task GetFileForUser_WithCompanyFile_MapsMachineToNull()
    {
        _invoker.Respond = _ => DocumentCatalogTestData.CreateInfo(machineId: null);

        var file = await _catalog.GetFileForUserAsync(
            DocumentCatalogTestData.FileId, TestContext.Current.CancellationToken);

        file!.MachineId.Should().BeNull();
    }

    [Fact]
    public async Task GetFileForUser_WhenDocumentAnswersNotFound_ReturnsNull()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.NotFound, detail: "missing"));

        var file = await _catalog.GetFileForUserAsync(
            DocumentCatalogTestData.FileId, TestContext.Current.CancellationToken);

        file.Should().BeNull();
    }

    [Fact]
    public async Task GetFileForUser_WhenDocumentIsUnavailable_Propagates()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.Unavailable, detail: "down"));

        var act = () => _catalog.GetFileForUserAsync(
            DocumentCatalogTestData.FileId, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<RpcException>()
            .Where(exception => exception.StatusCode == StatusCode.Unavailable);
    }

    [Fact]
    public async Task GetFileLinkForWorker_WithFile_MapsFileAndLink()
    {
        _invoker.Respond = _ => DocumentCatalogTestData.CreateLink(DocumentCatalogTestData.MachineId);

        var link = await _catalog.GetFileLinkForWorkerAsync(
            DocumentCatalogTestData.FileId, TestContext.Current.CancellationToken);

        link.Should().NotBeNull();
        link!.File.FileId.Should().Be(DocumentCatalogTestData.FileId);
        link.File.MachineId.Should().Be(DocumentCatalogTestData.MachineId);
        link.Url.Should().Be(DocumentCatalogTestData.LinkUrl);
        link.ExpiresAt.Should().Be(DocumentCatalogTestData.ExpiresAt);
    }

    [Fact]
    public async Task GetFileLinkForWorker_WhenDocumentAnswersNotFound_ReturnsNull()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.NotFound, detail: "missing"));

        var link = await _catalog.GetFileLinkForWorkerAsync(
            DocumentCatalogTestData.FileId, TestContext.Current.CancellationToken);

        link.Should().BeNull();
    }

    [Fact]
    public async Task GetFileLinkForWorker_Always_EntersTheDownstreamScopeForTheCallOnly()
    {
        _invoker.Respond = _ => DocumentCatalogTestData.CreateLink(machineId: null);

        await _catalog.GetFileLinkForWorkerAsync(
            DocumentCatalogTestData.FileId, TestContext.Current.CancellationToken);

        _invoker.ScopesAtCall.Should().ContainSingle().Which.Should().Equal(WorkerScope);
        _holder.Current.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFileLinkForWorker_WhenTheCallFails_LeavesTheDownstreamScope()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.Unavailable, detail: "down"));

        var act = () => _catalog.GetFileLinkForWorkerAsync(
            DocumentCatalogTestData.FileId, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RpcException>();
        _holder.Current.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFileForUser_Always_EntersNoDownstreamScope()
    {
        _invoker.Respond = _ => DocumentCatalogTestData.CreateInfo(machineId: null);

        await _catalog.GetFileForUserAsync(DocumentCatalogTestData.FileId, TestContext.Current.CancellationToken);

        _invoker.ScopesAtCall.Should().ContainSingle().Which.Should().BeEmpty();
    }
}
