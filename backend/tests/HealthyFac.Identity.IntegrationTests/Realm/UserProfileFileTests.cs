using System.Text.Json.Nodes;
using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.Realm;

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
