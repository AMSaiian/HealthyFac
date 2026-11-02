using HealthyFac.Auth.Application.Identities;

namespace HealthyFac.Auth.IntegrationTests.Identities.Data;

/// <summary>
/// Plays the identity provider for the failure cases that a real Keycloak cannot produce on demand: an outage, a
/// stop between two steps, and a parallel run that creates the user first. It keeps its users in memory.
/// </summary>
public sealed class FakeIdentityAdmin : IIdentityAdmin
{
    private readonly Dictionary<Guid, Guid> _userIdsByEmployee = new();
    private readonly HashSet<Guid> _userIdsWithPassword = new();
    private readonly List<Guid> _passwordEmailUserIds = new();

    private bool _isCreateFailurePending;
    private bool _isCreateRacePending;
    private bool _isDisableFailurePending;
    private readonly List<Guid> _disabledUserIds = new();
    private readonly List<Guid> _loggedOutUserIds = new();

    public Guid OrganizationId { get; } = Guid.CreateVersion7();

    public bool IsUnavailable { get; set; }

    public bool IsRealmRoleMissing { get; set; }

    public int CreatedUserCount { get; private set; }

    public IReadOnlyList<Guid> PasswordEmailUserIds => _passwordEmailUserIds;

    public void FailOnceAfterCreate() => _isCreateFailurePending = true;

    public IReadOnlyList<Guid> DisabledUserIds => _disabledUserIds;

    public IReadOnlyList<Guid> LoggedOutUserIds => _loggedOutUserIds;

    public void LoseCreateRaceOnce() => _isCreateRacePending = true;

    public void FailOnceOnDisable() => _isDisableFailurePending = true;

    public Guid AddUserWithPassword(Guid employeeId)
    {
        var userId = Guid.CreateVersion7();
        _userIdsByEmployee[employeeId] = userId;
        _userIdsWithPassword.Add(userId);

        return userId;
    }

    public Task<Guid?> FindOrganizationIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        ThrowWhenUnavailable();

        return Task.FromResult<Guid?>(OrganizationId);
    }

    public Task<Guid?> FindUserIdByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        ThrowWhenUnavailable();

        return Task.FromResult(_userIdsByEmployee.TryGetValue(employeeId, out var userId) ? userId : (Guid?)null);
    }

    public Task<Guid> CreateUserAsync(NewIdentity identity, CancellationToken cancellationToken)
    {
        ThrowWhenUnavailable();

        var userId = Guid.CreateVersion7();
        _userIdsByEmployee[identity.EmployeeId] = userId;
        CreatedUserCount++;

        if (_isCreateRacePending)
        {
            _isCreateRacePending = false;

            throw IdentityAdminErrors.IdentityConflict(identity.EmployeeId);
        }

        if (_isCreateFailurePending)
        {
            _isCreateFailurePending = false;

            throw new HttpRequestException(ProvisioningTestData.KeycloakUnavailableMessage);
        }

        return Task.FromResult(userId);
    }

    public Task AddOrganizationMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        ThrowWhenUnavailable();

        return Task.CompletedTask;
    }

    public Task AssignRealmRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken)
    {
        ThrowWhenUnavailable();

        if (IsRealmRoleMissing)
        {
            throw IdentityAdminErrors.RealmRoleNotFound(roleName);
        }

        return Task.CompletedTask;
    }

    public Task<bool> HasPasswordAsync(Guid userId, CancellationToken cancellationToken)
    {
        ThrowWhenUnavailable();

        return Task.FromResult(_userIdsWithPassword.Contains(userId));
    }

    public Task SendSetPasswordEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        ThrowWhenUnavailable();
        _passwordEmailUserIds.Add(userId);

        return Task.CompletedTask;
    }

    public Task DisableUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        ThrowWhenUnavailable();

        if (_isDisableFailurePending)
        {
            _isDisableFailurePending = false;

            throw new HttpRequestException(ProvisioningTestData.KeycloakUnavailableMessage);
        }

        _disabledUserIds.Add(userId);

        return Task.CompletedTask;
    }

    public Task LogoutUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        ThrowWhenUnavailable();
        _loggedOutUserIds.Add(userId);

        return Task.CompletedTask;
    }

    private void ThrowWhenUnavailable()
    {
        if (IsUnavailable)
        {
            throw new HttpRequestException(ProvisioningTestData.KeycloakUnavailableMessage);
        }
    }
}
