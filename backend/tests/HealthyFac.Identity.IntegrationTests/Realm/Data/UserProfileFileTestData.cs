using System.Text.Json.Nodes;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

public static class UserProfileFileTestData
{
    public const string OutOfDateMessage =
        "the realm file must embed docker/keycloak/user-profile.json. Run docker/keycloak/embed-user-profile.sh";

    private const string RealmFileName = "healthyfac-realm.json";
    private const string RealmFileDirectory = "import";
    private const string UserProfileFileName = "user-profile.json";
    private const string UserProfileProviderKey = "org.keycloak.userprofile.UserProfileProvider";
    private const string ConfigKey = "config";
    private const string ProfileConfigKey = "kc.user.profile.config";

    public static JsonNode ReadSourceProfile() =>
        JsonNode.Parse(File.ReadAllText(KeycloakFiles.Find(UserProfileFileName)))!;

    public static JsonNode ReadEmbeddedProfile()
    {
        var realm = JsonNode.Parse(File.ReadAllText(KeycloakFiles.Find(RealmFileDirectory, RealmFileName)))!;
        var profileText = realm["components"]![UserProfileProviderKey]![0]![ConfigKey]![ProfileConfigKey]![0]!
            .GetValue<string>();

        return JsonNode.Parse(profileText)!;
    }
}
