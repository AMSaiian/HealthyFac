using System.Text.Json;
using HealthyFac.Seeder.Accounts;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Features;

namespace HealthyFac.Seeder.UnitTests.Accounts;

[Feature(TestFeatures.Seeding)]
[Feature(TestFeatures.Identity)]
public sealed class RealmPasswordPolicyTests
{
    private const string PolicyProperty = "passwordPolicy";

    [Fact(Explicit = true)]
    public void Text_ComparedWithTheRealmFile_IsEqual()
    {
        var realmFile = KeycloakFiles.Find("realm", "healthyfac-realm.json");

        using var realm = JsonDocument.Parse(File.ReadAllText(realmFile));

        realm.RootElement.GetProperty(PolicyProperty).GetString().Should().Be(RealmPasswordPolicy.Text);
    }
}
