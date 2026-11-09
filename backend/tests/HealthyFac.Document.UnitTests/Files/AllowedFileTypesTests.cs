using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.UnitTests.Files.Data;

namespace HealthyFac.Document.UnitTests.Files;

public sealed class AllowedFileTypesTests
{
    [Fact]
    public void All_Always_HoldsTheStoredExtensionsAndMediaTypes()
    {
        var stored = AllowedFileTypes.All.Select(type => (type.Extension, type.MediaType)).ToList();

        stored.Should().Equal(AllowedFileTypesTestData.StoredTypes);
    }
}
