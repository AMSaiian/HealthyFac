using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.Realm;

public sealed class RealmRoleFileTests
{
    [Theory]
    [MemberData(nameof(RealmRoleFileTestData.RolesWithMachinePermissions), MemberType = typeof(RealmRoleFileTestData))]
    public void RealmRole_InRealmFile_HasMachinePermissions(string roleName)
    {
        var permissions = RealmRoleFileTestData.ReadRolePermissions(roleName);

        permissions.Should().Contain(RealmTestData.MachinePermissions);
    }

    [Theory]
    [MemberData(nameof(RealmRoleFileTestData.RolesWithMachinePermissions), MemberType = typeof(RealmRoleFileTestData))]
    public void RealmRole_InRealmFile_HasTelemetryPermissions(string roleName)
    {
        var permissions = RealmRoleFileTestData.ReadRolePermissions(roleName, RealmTestData.TelemetryClientId);

        permissions.Should().BeEquivalentTo(RealmTestData.TelemetryPermissions);
    }

    [Theory]
    [MemberData(nameof(RealmRoleFileTestData.RolesWithMachinePermissions), MemberType = typeof(RealmRoleFileTestData))]
    public void RealmRole_InRealmFile_HasDocumentPermissions(string roleName)
    {
        var permissions = RealmRoleFileTestData.ReadRolePermissions(roleName, RealmTestData.DocumentClientId);

        permissions.Should().BeEquivalentTo(RealmTestData.DocumentPermissions);
    }

    [Theory]
    [MemberData(nameof(RealmRoleFileTestData.RolesWithMachinePermissions), MemberType = typeof(RealmRoleFileTestData))]
    public void RealmRole_InRealmFile_HasInsightsPermissions(string roleName)
    {
        var permissions = RealmRoleFileTestData.ReadRolePermissions(roleName, RealmTestData.InsightsClientId);

        permissions.Should().BeEquivalentTo(RealmTestData.InsightsPermissions);
    }

    [Fact]
    public void InsightsServiceRole_InRealmFile_ReadsSignalsAndWorkOrders()
    {
        var telemetry = RealmRoleFileTestData.ReadRolePermissions(
            RealmTestData.InsightsServiceRoleName,
            RealmTestData.TelemetryClientId);

        var core = RealmRoleFileTestData.ReadRolePermissions(
            RealmTestData.InsightsServiceRoleName,
            RealmTestData.CoreClientId);

        telemetry.Should().Contain(RealmTestData.SignalsReadPermission);
        core.Should().Equal(RealmTestData.WorkOrdersReadPermission);
    }

    [Fact]
    public void WorkOrdersScope_InRealmFile_HasCoreAudienceAndRoleMapping()
    {
        var realm = RealmRoleFileTestData.ReadRealm();

        var scope = realm["clientScopes"]!.AsArray()
            .Single(candidate => candidate!["name"]!.GetValue<string>()
                == RealmTestData.WorkOrdersScopeName)!;

        var audience = scope["protocolMappers"]!.AsArray().Single()!["config"]![
            "included.client.audience"]!.GetValue<string>();

        var mappedRoles = realm["clientScopeMappings"]![RealmTestData.CoreClientId]!.AsArray()
            .Single(candidate => candidate!["clientScope"]!.GetValue<string>()
                == RealmTestData.WorkOrdersScopeName)!["roles"]!.AsArray()
            .Select(role => role!.GetValue<string>());

        audience.Should().Be(RealmTestData.CoreClientId);
        mappedRoles.Should().Equal(RealmTestData.WorkOrdersReadPermission);
    }

    [Fact]
    public void InsightsClient_InRealmFile_OffersTheWorkOrdersScopeAndMapsSignals()
    {
        var realm = RealmRoleFileTestData.ReadRealm();

        var optionalScopes = realm["clients"]!.AsArray()
            .Single(candidate => candidate!["clientId"]!.GetValue<string>()
                == RealmTestData.InsightsClientId)!["optionalClientScopes"]!.AsArray()
            .Select(scope => scope!.GetValue<string>());

        var telemetryRoles = realm["clientScopeMappings"]![RealmTestData.TelemetryClientId]!.AsArray()
            .Single(candidate => candidate!["clientScope"]!.GetValue<string>()
                == RealmTestData.TelemetryScopeName)!["roles"]!.AsArray()
            .Select(role => role!.GetValue<string>());

        optionalScopes.Should().Contain(RealmTestData.WorkOrdersScopeName);
        telemetryRoles.Should().Contain(RealmTestData.SignalsReadPermission);
    }
}
