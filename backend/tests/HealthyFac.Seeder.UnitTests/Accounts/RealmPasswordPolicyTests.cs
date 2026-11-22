using System.Text.Json;
using HealthyFac.Seeder.Accounts;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Seeder.UnitTests.Accounts;

public sealed class RealmPasswordPolicyTests
{
    private const string PolicyProperty = "passwordPolicy";

    [Fact]
    public void Text_ComparedWithTheRealmFile_IsEqual()
    {
        var realmFile = KeycloakFiles.Find("realm", "healthyfac-realm.json");

        using var realm = JsonDocument.Parse(File.ReadAllText(realmFile));

        realm.RootElement.GetProperty(PolicyProperty).GetString().Should().Be(RealmPasswordPolicy.Text);
    }
}
