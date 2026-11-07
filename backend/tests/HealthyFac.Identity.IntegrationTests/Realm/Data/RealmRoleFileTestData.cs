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

    public static IReadOnlyList<string> ReadRolePermissions(string roleName) =>
        ReadRolePermissions(roleName, RealmTestData.CoreClientId);

    public static IReadOnlyList<string> ReadRolePermissions(string roleName, string clientId)
    {
        var realm = JsonNode.Parse(File.ReadAllText(KeycloakFiles.Find(RealmFileDirectory, RealmFileName)))!;
        var role = realm[propertyName: "roles"]![propertyName: "realm"]!.AsArray()
            .Single(candidate => candidate![propertyName: "name"]!.GetValue<string>() == roleName)!;

        return role[propertyName: "composites"]?[propertyName: "client"]?[clientId]
                ?.AsArray()
                .Select(permission => permission!.GetValue<string>())
                .ToList()
            ?? [];
    }
}
