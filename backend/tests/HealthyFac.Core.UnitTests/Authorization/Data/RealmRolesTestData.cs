using System.Text.Json.Nodes;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Core.UnitTests.Authorization.Data;

public static class RealmRolesTestData
{
    private const string RealmFileDirectory = "import";
    private const string RealmFileName = "healthyfac-realm.json";
    private const string CoreClientId = "healthyfac-core";
    private const string RolesKey = "roles";
    private const string ClientKey = "client";
    private const string RealmKey = "realm";
    private const string NameKey = "name";
    private const string CompositesKey = "composites";

    public static IReadOnlyList<string> ReadCoreClientRoles() =>
        ReadRoles()[ClientKey]![CoreClientId]!.AsArray()
            .Select(role => role![NameKey]!.GetValue<string>())
            .ToList();

    public static IReadOnlyList<JobRole> ReadJobRoles() =>
        ReadRoles()[RealmKey]!.AsArray()
            .Select(role => new JobRole
            {
                Name = role![NameKey]!.GetValue<string>(),
                CorePermissions = role[CompositesKey]?[ClientKey]?[CoreClientId]
                        ?.AsArray()
                        .Select(permission => permission!.GetValue<string>())
                        .ToList()
                    ?? [],
            })
            .ToList();

    private static JsonNode ReadRoles() =>
        JsonNode.Parse(File.ReadAllText(KeycloakFiles.Find(RealmFileDirectory, RealmFileName)))![RolesKey]!;
}

public sealed record JobRole
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> CorePermissions { get; init; }
}
