using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Infrastructure.Companies.Seeding;
using HealthyFac.Core.Infrastructure.Employees.Seeding;
using HealthyFac.Core.Infrastructure.Machines.Seeding;
using HealthyFac.Core.Infrastructure.WorkOrders.Seeding;
using HealthyFac.Core.UnitTests.Fakers.Data;
using HealthyFac.Shared.Infrastructure.RichText;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.Fakers;

[Feature(TestFeatures.Seeding)]
public sealed class FakerInvariantTests
{
    private readonly HtmlSanitizerRichTextPolicy _richTextPolicy = new();

    [Fact]
    public void Generate_FromTheCompanyFaker_GivesNamesAndKnownZonesWithinTheLimits()
    {
        var companies = new CompanyFaker(FakerInvariantTestData.Seed).Generate(FakerInvariantTestData.ItemCount);

        companies.Should()
            .OnlyContain(company => !string.IsNullOrWhiteSpace(company.Name)
                && company.Name.Length <= CompanyValidationConstants.NameMaxLength
                && company.TimeZoneId.Length <= CompanyValidationConstants.TimeZoneIdMaxLength);

        companies.Select(company => TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId))
            .Should()
            .NotBeEmpty()
            .And.OnlyContain(zone => zone != null);
    }

    [Fact]
    public void Generate_FromTheInviteFakerWithProfile_GivesInvitesThatPassTheCreateValidator()
    {
        var invites = EmployeeInviteFaker.WithProfile(
                FakerInvariantTestData.CompanyId,
                FakerInvariantTestData.RoleId,
                FakerInvariantTestData.Now,
                FakerInvariantTestData.InviteLifetime,
                FakerInvariantTestData.Seed)
            .Generate(FakerInvariantTestData.ItemCount);

        AssertInvitesAreValid(invites);
    }

    [Fact]
    public void Generate_FromTheOpenInviteFaker_GivesInvitesThatPassTheCreateValidator()
    {
        var invites = EmployeeInviteFaker.Open(
                FakerInvariantTestData.CompanyId,
                FakerInvariantTestData.RoleId,
                FakerInvariantTestData.Now,
                FakerInvariantTestData.InviteLifetime,
                FakerInvariantTestData.Seed)
            .Generate(FakerInvariantTestData.ItemCount);

        AssertInvitesAreValid(invites);
    }

    [Fact]
    public void Generate_FromTheMachineFaker_GivesMachinesThatPassTheCreateValidatorWithUniqueSerialNumbers()
    {
        var machines = CreateMachines();

        var validator = new CreateMachineRequestValidator(_richTextPolicy);

        var failures = machines
            .SelectMany(machine => validator.Validate(
                    new CreateMachineRequest
                    {
                        CompanyId = machine.CompanyId,
                        Name = machine.Name,
                        SerialNumber = machine.SerialNumber,
                        Description = machine.Description,
                        ExpectedGoLiveOn = machine.ExpectedGoLiveOn,
                    })
                .Errors)
            .ToList();

        failures.Should().BeEmpty();
        machines.Select(machine => machine.SerialNumber).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Generate_FromTheMachineFaker_StartsEveryStateHistoryOnOrAfterTheGoLiveDayAndBeforeNow()
    {
        var machines = CreateMachines();

        var zone = TimeZoneInfo.FindSystemTimeZoneById(FakerInvariantTestData.CompanyZone);

        var historyRows = machines
            .SelectMany(machine => machine.StateHistory.Select(row => (machine.ExpectedGoLiveOn, row.StartedAt)))
            .ToList();

        historyRows.Should().NotBeEmpty();

        historyRows.Should()
            .OnlyContain(row => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(row.StartedAt, zone))
                >= row.ExpectedGoLiveOn
                && row.StartedAt < FakerInvariantTestData.Now);
    }

    [Fact]
    public void Generate_FromTheWorkOrderFaker_GivesOrdersThatPassTheCreateValidator()
    {
        var machineIds = new List<Guid> { Guid.CreateVersion7(), Guid.CreateVersion7() };
        var faker = new WorkOrderFaker(FakerInvariantTestData.CompanyId, machineIds, FakerInvariantTestData.Seed);

        var orders = faker.Generate(FakerInvariantTestData.ItemCount);

        var validator = new CreateWorkOrderRequestValidator(_richTextPolicy);

        var failures = orders
            .SelectMany(order => validator.Validate(
                    new CreateWorkOrderRequest
                    {
                        CompanyId = order.CompanyId,
                        Scope = AccessScope.Company,
                        Title = order.Title,
                        Description = order.Description,
                        Priority = order.Priority,
                        Category = order.Category,
                        MachineId = order.MachineId,
                        EstimateMinutes = (int)order.Estimate.TotalMinutes,
                        FileIds = [],
                    })
                .Errors)
            .ToList();

        failures.Should().BeEmpty();

        orders.Should()
            .OnlyContain(order => order.Title.Length <= WorkOrderValidationConstants.TitleMaxLength
                && order.Description!.StartsWith("<p>")
                && order.Description.EndsWith("</p>"));
    }

    private static void AssertInvitesAreValid(List<EmployeeInvite> invites)
    {
        var validator = new CreateEmployeeInviteRequestValidator();

        var failures = invites
            .SelectMany(invite => validator.Validate(
                    new CreateEmployeeInviteRequest
                    {
                        CompanyId = invite.CompanyId,
                        Email = invite.Email,
                        RoleCode = FakerInvariantTestData.InviteRole,
                        FirstName = invite.FirstName,
                        LastName = invite.LastName,
                    })
                .Errors)
            .ToList();

        failures.Should().BeEmpty();
    }

    private static List<Machine> CreateMachines() =>
        new MachineFaker(
            FakerInvariantTestData.CompanyId,
            FakerInvariantTestData.CompanyZone,
            FakerInvariantTestData.Now,
            FakerInvariantTestData.Seed).Generate(FakerInvariantTestData.ItemCount);
}
