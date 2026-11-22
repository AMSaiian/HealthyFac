namespace HealthyFac.Seeder.People;

/// <summary>The content of <c>company.json</c>: the company, the demo password, the people, and their shifts.</summary>
public sealed class CompanyFile
{
    public required CompanySeed Company { get; init; }

    /// <summary>The password of all demo accounts. The seed check applies the realm policy to it.</summary>
    public required string Password { get; init; }

    public required IReadOnlyList<PersonSeed> People { get; init; }

    public required IReadOnlyList<OpenInviteSeed> OpenInvites { get; init; }

    public required IReadOnlyList<ShiftOverrideSeed> ShiftOverrides { get; init; }
}
