using System.Text.Json.Nodes;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Core.UnitTests.Authorization.Data;

public static class RealmRolesTestData
{
    private const string RealmFileDirectory = "realm";
    private const string RealmFileName = "healthyfac-realm.json";
    private const string CoreClientId = "healthyfac-core";
    private const string RolesKey = "roles";
    private const string ClientKey = "client";
    private const string RealmKey = "realm";
    private const string NameKey = "name";
    private const string CompositesKey = "composites";

    /// <summary>The realm roles of service accounts. A service role is no job role, also when it reads Core.</summary>
    private static readonly string[] ServiceRoleNames = ["insights-service"];

    public static IReadOnlyList<string> ReadCoreClientRoles() =>
        [.. ReadRoles()[ClientKey]![CoreClientId]!.AsArray().Select(role => role![NameKey]!.GetValue<string>())];

    public static IReadOnlyList<JobRole> ReadJobRoles() =>
    [
        .. ReadRoles()[RealmKey]!.AsArray()
            .Where(role => !ServiceRoleNames.Contains(role![NameKey]!.GetValue<string>()))
            .Select(role => new JobRole
            {
                Name = role![NameKey]!.GetValue<string>(),
                CorePermissions = role[CompositesKey]?[ClientKey]?[CoreClientId]
                        ?.AsArray()
                        .Select(permission => permission!.GetValue<string>())
                        .ToList()
                    ?? [],
            }),
    ];

    private static JsonNode ReadRoles() =>
        JsonNode.Parse(File.ReadAllText(KeycloakFiles.Find(RealmFileDirectory, RealmFileName)))![RolesKey]!;
}

public sealed record JobRole
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> CorePermissions { get; init; }
}
