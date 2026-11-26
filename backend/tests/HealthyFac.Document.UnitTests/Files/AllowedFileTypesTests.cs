using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.UnitTests.Files.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Document.UnitTests.Files;

[Feature(TestFeatures.Files)]
public sealed class AllowedFileTypesTests
{
    [Fact]
    public void All_Always_HoldsTheStoredExtensionsAndMediaTypes()
    {
        var stored = AllowedFileTypes.All.Select(type => (type.Extension, type.MediaType)).ToList();

        stored.Should().Equal(AllowedFileTypesTestData.StoredTypes);
    }
}
