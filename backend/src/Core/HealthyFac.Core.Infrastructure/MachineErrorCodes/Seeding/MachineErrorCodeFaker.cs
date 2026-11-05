using Bogus;
using HealthyFac.Core.Domain.MachineErrorCodes;
using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Infrastructure.MachineErrorCodes.Seeding;

/// <summary>
/// Generates development error codes of one machine. The code takes the number of its position, so the codes of one
/// machine are unique, as the unique index requires. Some descriptions are empty and some are one allowed paragraph.
/// </summary>
internal sealed class MachineErrorCodeFaker : Faker<MachineErrorCode>
{
    private const string CodeFormat = "E-{0:000}";
    private const string DescriptionFormat = "<p>{0}</p>";

    /// <summary>
    /// Declares the error code shape.
    /// </summary>
    /// <param name="machine">The machine that owns the error codes.</param>
    /// <param name="seed">The random seed, so every run produces the same error codes.</param>
    public MachineErrorCodeFaker(Machine machine, int seed)
    {
        UseSeed(seed);
        CustomInstantiator(faker => new MachineErrorCode(
            machine.Id,
            string.Format(CodeFormat, faker.IndexFaker + 1),
            faker.Random.Bool() ? string.Format(DescriptionFormat, faker.Lorem.Sentence()) : null));
    }
}
