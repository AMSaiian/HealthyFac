using System.Text.Json.Nodes;
using HealthyFac.Identity.IntegrationTests.Realm.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Identity.IntegrationTests.Realm;

[Feature(TestFeatures.Identity)]
public sealed class UserProfileFileTests
{
    [Fact]
    public void UserProfile_InRealmFile_EqualsSourceFile()
    {
        var source = UserProfileFileTestData.ReadSourceProfile();
        var embedded = UserProfileFileTestData.ReadEmbeddedProfile();

        var isEqual = JsonNode.DeepEquals(source, embedded);

        isEqual.Should().BeTrue(UserProfileFileTestData.OutOfDateMessage);
    }
}
