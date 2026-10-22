using Bogus;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Machines;

namespace HealthyFac.Core.Infrastructure.Machines.Seeding;

/// <summary>
/// Generates development machines of one department. The machine takes the company of the department, as the
/// composite department key requires.
/// </summary>
internal sealed class MachineFaker : Faker<Machine>
{
    /// <summary>
    /// Declares the machine shape.
    /// </summary>
    /// <param name="department">The saved department that owns the machines.</param>
    /// <param name="machineTypes">The machine types to pick from. The name appends a two-digit number.</param>
    /// <param name="serialNumberPattern">
    /// The Bogus replacement pattern of serial numbers: <c>#</c> becomes a digit and <c>?</c> becomes a letter.
    /// </param>
    /// <param name="seed">The random seed, so every run produces the same machines.</param>
    public MachineFaker(
        Department department,
        IReadOnlyList<string> machineTypes,
        string serialNumberPattern,
        int seed
    )
    {
        UseSeed(seed);
        RuleFor(
            machine => machine.Name,
            faker => $"{faker.PickRandom<string>(machineTypes)} {faker.IndexFaker + 1:00}");
        RuleFor(machine => machine.SerialNumber, faker => faker.Random.Replace(serialNumberPattern));
        RuleFor(machine => machine.CompanyId, _ => department.CompanyId);
        RuleFor(machine => machine.DepartmentId, _ => department.Id);
    }
}
