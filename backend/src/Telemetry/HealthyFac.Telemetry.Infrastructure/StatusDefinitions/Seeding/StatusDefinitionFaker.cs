using Bogus;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.StatusDefinitions;

namespace HealthyFac.Telemetry.Infrastructure.StatusDefinitions.Seeding;

/// <summary>
/// Generates development status definitions of one machine. The code takes the number of its position, so the codes of
/// one machine are unique, as the unique index requires. The severity cycles through all severities. Some descriptions
/// are empty and some are one allowed paragraph.
/// </summary>
internal sealed class StatusDefinitionFaker : Faker<StatusDefinition>
{
    private const string CodeFormat = "E-{0:000}";
    private const string DescriptionFormat = "<p>{0}</p>";

    /// <summary>
    /// Declares the status definition shape.
    /// </summary>
    /// <param name="registration">The registration of the machine that owns the status definitions.</param>
    /// <param name="seed">The random seed, so every run produces the same status definitions.</param>
    public StatusDefinitionFaker(MachineRegistration registration, int seed)
    {
        UseSeed(seed);
        CustomInstantiator(faker => new StatusDefinition(
            registration.Id,
            string.Format(CodeFormat, faker.IndexFaker + 1),
            StatusSeverity.All[faker.IndexFaker % StatusSeverity.All.Count],
            faker.Random.Bool() ? string.Format(DescriptionFormat, faker.Lorem.Sentence()) : null));
    }
}
