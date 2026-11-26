using HealthyFac.Shared.Infrastructure.RichText;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Infrastructure.StatusDefinitions.Seeding;
using HealthyFac.Telemetry.UnitTests.Fakers.Data;

namespace HealthyFac.Telemetry.UnitTests.Fakers;

public sealed class StatusDefinitionFakerTests
{
    [Fact]
    public void Generate_FromTheStatusDefinitionFaker_GivesDefinitionsThatPassTheCreateValidatorWithUniqueCodes()
    {
        var faker = new StatusDefinitionFaker(
            StatusDefinitionFakerTestData.CreateRegistration(),
            StatusDefinitionFakerTestData.Seed);

        var definitions = faker.Generate(StatusDefinitionFakerTestData.ItemCount);

        var validator = new CreateStatusDefinitionRequestValidator(new HtmlSanitizerRichTextPolicy());

        var failures = definitions
            .SelectMany(definition => validator.Validate(
                    new CreateStatusDefinitionRequest
                    {
                        CompanyId = StatusDefinitionFakerTestData.CompanyId,
                        MachineId = StatusDefinitionFakerTestData.MachineId,
                        Code = definition.Code,
                        Severity = definition.Severity,
                        Description = definition.Description,
                    })
                .Errors)
            .ToList();

        failures.Should().BeEmpty();
        definitions.Select(definition => definition.Code).Should().OnlyHaveUniqueItems();
    }
}
