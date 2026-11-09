using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.UnitTests.Files.Data;

namespace HealthyFac.Document.UnitTests.Files;

public sealed class FileContentDispositionTests
{
    [Fact]
    public void Build_WithImage_ReturnsInline()
    {
        var header = FileContentDisposition.Build(
            AllowedFileTypes.FindByExtension(extension: "png")!, fileName: "scheme");

        header.Should().Be(expected: "inline");
    }

    [Theory]
    [MemberData(
        nameof(FileContentDispositionTestData.AttachmentNames), MemberType = typeof(FileContentDispositionTestData))]
    public void Build_WithDocument_ReturnsAttachmentWithTheName(string name, string extension, string expected)
    {
        var header = FileContentDisposition.Build(AllowedFileTypes.FindByExtension(extension)!, name);

        header.Should().Be(expected);
    }

    [Theory]
    [MemberData(
        nameof(FileContentDispositionTestData.HostileNames), MemberType = typeof(FileContentDispositionTestData))]
    public void Build_WithHostileName_KeepsTheHeaderOnOneLineAndTheFallbackNameSafe(string name)
    {
        var header = FileContentDisposition.Build(AllowedFileTypes.FindByExtension(extension: "pdf")!, name);

        header.Should().NotContainAny("\r", "\n");
        header.Should().StartWith(expected: "attachment; filename=");
        header.Split(separator: "; filename*=")[0].Should().NotContainAny("%", "\\", "п");
    }
}
