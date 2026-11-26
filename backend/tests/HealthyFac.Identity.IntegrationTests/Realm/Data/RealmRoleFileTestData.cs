using System.Text.Json.Nodes;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

public static class RealmRoleFileTestData
{
    private const string RealmFileName = "healthyfac-realm.json";
    private const string RealmFileDirectory = "realm";

    public static TheoryData<string> RolesWithMachinePermissions =>
    [
        RealmTestData.WorkerRoleName,
        RealmTestData.SupervisorRoleName,
    ];

    public static JsonNode ReadRealm() =>
        JsonNode.Parse(File.ReadAllText(KeycloakFiles.Find(RealmFileDirectory, RealmFileName)))!;

    public static IReadOnlyList<string> ReadRolePermissions(string roleName) =>
        ReadRolePermissions(roleName, RealmTestData.CoreClientId);

    public static IReadOnlyList<string> ReadRolePermissions(string roleName, string clientId)
    {
        var realm = ReadRealm();

        var role = realm["roles"]!["realm"]!.AsArray()
            .Single(candidate => candidate!["name"]!.GetValue<string>() == roleName)!;

        return role["composites"]?["client"]?[clientId]
                ?.AsArray()
                .Select(permission => permission!.GetValue<string>())
                .ToList()
            ?? [];
    }
}
