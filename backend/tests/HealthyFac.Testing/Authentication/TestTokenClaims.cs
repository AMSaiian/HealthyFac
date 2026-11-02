namespace HealthyFac.Testing.Authentication;

/// <summary>
/// Describes the claims of a test access token. The defaults give a valid token of a supervisor: one organization,
/// an employee ID, the audience of Core, and the permissions. A test overrides only the claim it checks.
/// </summary>
public sealed record TestTokenClaims
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid DefaultSubject = Guid.Parse("0190f3a0-0000-7000-8000-000000000002");
    public static readonly Guid DefaultEmployeeId = Guid.Parse("0190f3a0-0000-7000-8000-000000000003");

    /// <summary>The <c>sub</c> claim. Set it to <see langword="null"/> to leave the claim out.</summary>
    public Guid? Subject { get; init; } = DefaultSubject;

    /// <summary>The <c>organization</c> claim, one text value for each organization.</summary>
    public IReadOnlyList<string> Organizations { get; init; } = [DefaultCompanyId.ToString()];

    /// <summary>The <c>employee_id</c> claim. Set it to <see langword="null"/> to leave the claim out.</summary>
    public string? EmployeeId { get; init; } = DefaultEmployeeId.ToString();

    /// <summary>The <c>permissions</c> claim, one text value for each permission.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = TestTokenFactory.SupervisorPermissions;

    public string Issuer { get; init; } = TestTokenFactory.Issuer;

    public string Audience { get; init; } = TestTokenFactory.Audience;

    /// <summary>The <c>iat</c> claim. The default is the creation time of the claims object.</summary>
    public DateTimeOffset IssuedAt { get; init; } = DateTimeOffset.UtcNow;

    public TimeSpan Lifetime { get; init; } = TestTokenFactory.DefaultLifetime;
}
