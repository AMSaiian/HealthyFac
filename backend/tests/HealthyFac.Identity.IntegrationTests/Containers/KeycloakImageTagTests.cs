using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.Containers;

public sealed class KeycloakImageTagTests
{
    private const string FromPrefix = "FROM ";

    [Fact]
    public void FinalStage_OfKeycloakDockerfile_UsesTheTestImage()
    {
        var dockerfile = KeycloakFiles.Find("Dockerfile");
        var finalFromLine = File.ReadLines(dockerfile)
            .Last(line => line.StartsWith(FromPrefix, StringComparison.Ordinal));

        var image = finalFromLine[FromPrefix.Length..].Split(separator: ' ', StringSplitOptions.RemoveEmptyEntries)[0];

        image.Should().Be(TestImages.Keycloak);
    }
}
