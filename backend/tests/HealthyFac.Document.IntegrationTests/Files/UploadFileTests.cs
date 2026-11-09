using System.Diagnostics;
using FluentValidation;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Document.Application;
using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Application.Files.Inspection;
using HealthyFac.Document.Application.Files.Storage;
using HealthyFac.Document.Application.Files.Uploading;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Testing.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Cases = HealthyFac.Document.IntegrationTests.Files.Data.UploadFileTestData.OutOfScopeCases;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class UploadFileTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task UploadCompanyFile_WithPdf_StoresRefAndBlob()
    {
        var content = UploadFileTestData.Pdf;

        var response = await _fixture.UploadCompanyFileAsync(
            UploadFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(UploadFileTestData.PdfName, content, UploadFileTestData.PdfType),
            UploadFileTestData.Description,
            TestContext.Current.CancellationToken);

        var refs = await _fixture.LoadFileRefsAsync();
        var blob = await _fixture.ReadBlobAsync(response.Id);

        refs.Should().ContainSingle();
        refs.Single().Id.Should().Be(response.Id);
        refs.Single().CompanyId.Should().Be(UploadFileTestData.CompanyId);
        refs.Single().MachineId.Should().BeNull();
        refs.Single().Purpose.Should().Be(FilePurpose.Attachment);
        refs.Single().FileName.Should().Be(UploadFileTestData.StoredPdfName);
        refs.Single().Extension.Should().Be(expected: "pdf");
        refs.Single().ContentType.Should().Be(UploadFileTestData.PdfType);
        refs.Single().SizeBytes.Should().Be(content.Length);
        refs.Single().Sha256.Should().Equal(System.Security.Cryptography.SHA256.HashData(content));
        refs.Single().Description.Should().Be(UploadFileTestData.Description);
        blob.Should().Equal(content);
    }

    [Fact]
    public async Task UploadCompanyFile_WithRejectedType_StoresNothing()
    {
        var act = () => _fixture.UploadCompanyFileAsync(
            UploadFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.ExeNameAsPdf, UploadFileTestData.Exe, UploadFileTestData.PdfType));

        await act.Should().ThrowAsync<AppValidationException>().WithMessage($"*{FileErrors.TypeNotAllowed}*");

        var refs = await _fixture.LoadFileRefsAsync();
        var blobs = await _fixture.LoadBlobNamesAsync();

        refs.Should().BeEmpty();
        blobs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadCompanyFile_WithNameOnlyExtension_Returns400()
    {
        var act = () => _fixture.UploadCompanyFileAsync(
            UploadFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.NameOnlyExtension, UploadFileTestData.Pdf, UploadFileTestData.PdfType));

        await act.Should().ThrowAsync<ValidationException>();

        var refs = await _fixture.LoadFileRefsAsync();

        refs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadMachineFile_WithMachineInScope_StoresMachineId()
    {
        var machineId = await _fixture.RegisterMachineAsync(UploadFileTestData.CompanyId);

        await _fixture.UploadMachineFileAsync(
            UploadFileTestData.CompanyId,
            machineId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PdfName, UploadFileTestData.Pdf, UploadFileTestData.PdfType));

        var refs = await _fixture.LoadFileRefsAsync();

        refs.Single().MachineId.Should().Be(machineId);
        refs.Single().Purpose.Should().Be(FilePurpose.Attachment);
    }

    [Theory]
    [MemberData(nameof(UploadFileTestData.OutOfScopeMachines), MemberType = typeof(UploadFileTestData))]
    public async Task UploadMachineFile_OutsideScope_Throws404AndStoresNothing(string scenario)
    {
        var machineId = await ArrangeMachineAsync(scenario);

        var act = () => _fixture.UploadMachineFileAsync(
            UploadFileTestData.CompanyId,
            machineId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PdfName, UploadFileTestData.Pdf, UploadFileTestData.PdfType));

        await act.Should().ThrowAsync<NotFoundException>();

        var refs = await _fixture.LoadFileRefsAsync();
        var blobs = await _fixture.LoadBlobNamesAsync();

        refs.Should().BeEmpty();
        blobs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadEmbeddedImage_WithPng_StoresEmbedded()
    {
        await _fixture.UploadEmbeddedImageAsync(
            UploadFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PngName, UploadFileTestData.Png, UploadFileTestData.PngType));

        var refs = await _fixture.LoadFileRefsAsync();

        refs.Single().Purpose.Should().Be(FilePurpose.Embedded);
        refs.Single().MachineId.Should().BeNull();
        refs.Single().Extension.Should().Be(expected: "png");
    }

    [Fact]
    public async Task UploadEmbeddedImage_WithPdf_StoresNothing()
    {
        var act = () => _fixture.UploadEmbeddedImageAsync(
            UploadFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PdfName, UploadFileTestData.Pdf, UploadFileTestData.PdfType));

        await act.Should().ThrowAsync<AppValidationException>();

        var refs = await _fixture.LoadFileRefsAsync();
        var blobs = await _fixture.LoadBlobNamesAsync();

        refs.Should().BeEmpty();
        blobs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadCompanyFile_WithStorageFailure_ThrowsAndStoresNoRef()
    {
        using var scope = _fixture.CreateUserScope(UploadFileTestData.CompanyId);
        var handler = CreateHandler(scope, new FailingFileStorage());

        var act = () => handler.HandleAsync(
            CreateCompanyRequest(),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>();

        var refs = await _fixture.LoadFileRefsAsync();

        refs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadCompanyFile_CancelledAfterBlobWrite_StoresRef()
    {
        using var cancellation = new CancellationTokenSource();
        using var scope = _fixture.CreateUserScope(UploadFileTestData.CompanyId);
        var handler = CreateHandler(
            scope,
            new CancellingFileStorage(scope.ServiceProvider.GetRequiredService<IFileStorage>(), cancellation));

        var response = await handler.HandleAsync(CreateCompanyRequest(), cancellation.Token);

        var refs = await _fixture.LoadFileRefsAsync();
        var blobs = await _fixture.LoadBlobNamesAsync();

        cancellation.IsCancellationRequested.Should().BeTrue();
        refs.Single().Id.Should().Be(response.Id);
        blobs.Should().Equal(response.Id.ToString());
    }

    [Fact]
    public async Task UploadCompanyFile_CancelledBeforeBlobWrite_StoresNothing()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var scope = _fixture.CreateUserScope(UploadFileTestData.CompanyId);
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<UploadFileRequest, UploadFileResponse>>();

        var act = () => handler.HandleAsync(CreateCompanyRequest(), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        var refs = await _fixture.LoadFileRefsAsync();
        var blobs = await _fixture.LoadBlobNamesAsync();

        refs.Should().BeEmpty();
        blobs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadCompanyFile_WithAcceptedFile_RecordsInspectAndUploadSpansUnderTheUseCase()
    {
        using var capture = new SpanCapture(
            ObservabilityConstants.Sources.UseCases, ObservabilityConstants.Files.Source);
        using var parent = new Activity(operationName: "test").Start();

        var response = await _fixture.UploadCompanyFileAsync(
            UploadFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PdfName, UploadFileTestData.Pdf, UploadFileTestData.PdfType),
            cancellationToken: TestContext.Current.CancellationToken);

        var spans = capture.Spans.Where(span => span.TraceId == parent.TraceId).ToList();
        var useCase = spans.Single(span => span.OperationName == nameof(UploadFileRequest));
        var inspect = spans.Single(span => span.OperationName == ObservabilityConstants.Files.Spans.Inspect);
        var upload = spans.Single(span => span.OperationName == ObservabilityConstants.Files.Spans.BlobUpload);
        var tagValues = inspect.TagObjects.Concat(upload.TagObjects).Select(tag => tag.Value?.ToString()).ToList();

        inspect.ParentSpanId.Should().Be(useCase.SpanId);
        upload.ParentSpanId.Should().Be(useCase.SpanId);
        upload.GetTagItem(ObservabilityConstants.Files.Tags.Purpose).Should().Be(expected: "attachment");
        tagValues.Should().NotContain(UploadFileTestData.StoredPdfName);
        tagValues.Should().NotContain(response.Id.ToString());
    }

    [Fact]
    public async Task UploadCompanyFile_WithRejectedFile_RecordsNoBlobUploadSpan()
    {
        using var capture = new SpanCapture(ObservabilityConstants.Files.Source);
        using var parent = new Activity(operationName: "test").Start();

        var act = () => _fixture.UploadCompanyFileAsync(
            UploadFileTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.ExeNameAsPdf, UploadFileTestData.Exe, UploadFileTestData.PdfType));

        await act.Should().ThrowAsync<AppValidationException>();

        var spans = capture.Spans.Where(span => span.TraceId == parent.TraceId).ToList();

        spans.Should().ContainSingle(span => span.OperationName == ObservabilityConstants.Files.Spans.Inspect);
        spans.Single().GetTagItem(ObservabilityConstants.Files.Tags.Result).Should().Be(expected: "rejected");
        spans.Should().NotContain(span => span.OperationName == ObservabilityConstants.Files.Spans.BlobUpload);
    }

    private static UploadFileRequest CreateCompanyRequest() =>
        new()
        {
            CompanyId = UploadFileTestData.CompanyId,
            File = UploadFileTestData.CreateUpload(
                UploadFileTestData.PdfName, UploadFileTestData.Pdf, UploadFileTestData.PdfType),
        };

    private static UploadFileRequestHandler CreateHandler(IServiceScope scope, IFileStorage storage) =>
        new(
            new FileUploader(
                scope.ServiceProvider.GetRequiredService<IFileInspector>(),
                storage,
                scope.ServiceProvider.GetRequiredService<IDocumentDbContext>()));

    private async Task<Guid> ArrangeMachineAsync(string scenario)
    {
        switch (scenario)
        {
            case Cases.OtherCompany:
                return await _fixture.RegisterMachineAsync(UploadFileTestData.OtherCompanyId);
            case Cases.DeletedRegistration:
                var machineId = await _fixture.RegisterMachineAsync(UploadFileTestData.CompanyId);
                await _fixture.UnregisterMachineAsync(
                    new MachineDeleted
                    {
                        MachineId = machineId,
                        CompanyId = UploadFileTestData.CompanyId,
                    });

                return machineId;
            default:
                return Guid.CreateVersion7();
        }
    }
}
