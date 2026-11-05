using System.Text.Json.Nodes;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

public static class RealmRoleFileTestData
{
    private const string RealmFileName = "healthyfac-realm.json";
    private const string RealmFileDirectory = "realm";

    public static TheoryData<string> RolesWithMachinePermissions => new()
    {
        RealmTestData.WorkerRoleName,
        RealmTestData.SupervisorRoleName,
    };

    public static IReadOnlyList<string> ReadRolePermissions(string roleName)
    {
        var realm = JsonNode.Parse(File.ReadAllText(KeycloakFiles.Find(RealmFileDirectory, RealmFileName)))!;
        var role = realm["roles"]!["realm"]!.AsArray()
            .Single(candidate => candidate!["name"]!.GetValue<string>() == roleName)!;

        return role["composites"]?["client"]?[RealmTestData.CoreClientId]
                ?.AsArray()
                .Select(permission => permission!.GetValue<string>())
                .ToList()
            ?? [];
    }
}
