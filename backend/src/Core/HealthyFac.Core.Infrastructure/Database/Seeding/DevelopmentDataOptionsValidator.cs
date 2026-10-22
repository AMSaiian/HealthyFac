using FluentValidation;
using HealthyFac.Core.Application.Departments;
using HealthyFac.Core.Application.Machines;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Validates the development data settings at startup. It checks the effective lists, so a configured list and a
/// default list get the same checks, and every generated value fits the database constraints. It rejects forced
/// recreation outside the Development environment, so a wrong setting cannot delete data in a shared environment.
/// </summary>
public sealed class DevelopmentDataOptionsValidator : AbstractValidator<DevelopmentDataOptions>
{
    /// <summary>
    /// Declares the rules.
    /// </summary>
    /// <param name="environment">The host environment that decides whether forced recreation is allowed.</param>
    public DevelopmentDataOptionsValidator(IHostEnvironment environment)
    {
        RuleFor(options => options.IsRecreationForced)
            .Equal(false)
            .Unless(_ => environment.IsDevelopment())
            .WithMessage(SeedingValidationConstants.Messages.RecreationOnlyInDevelopment);

        RuleFor(options => options.CompanyCount)
            .InclusiveBetween(1, SeedingValidationConstants.Counts.MaxCompanies);

        RuleFor(options => options.DepartmentsPerCompany)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(options => options.GetDepartmentNames().Count);

        RuleFor(options => options.MachinesPerDepartment)
            .InclusiveBetween(0, SeedingValidationConstants.Counts.MaxPerDepartment);

        RuleFor(options => options.SupervisorsPerDepartment)
            .InclusiveBetween(0, SeedingValidationConstants.Counts.MaxSupervisorsPerDepartment);

        RuleFor(options => options.WorkersPerDepartment)
            .InclusiveBetween(0, SeedingValidationConstants.Counts.MaxPerDepartment);

        RuleFor(options => options.OpenInvitesPerCompany)
            .InclusiveBetween(0, SeedingValidationConstants.Counts.MaxOpenInvitesPerCompany);

        RuleFor(options => options.InviteLifetimeDays)
            .InclusiveBetween(
                SeedingValidationConstants.InviteLifetime.MinDays,
                SeedingValidationConstants.InviteLifetime.MaxDays);

        RuleFor(options => options.SerialNumberPattern)
            .NotEmpty()
            .MaximumLength(MachineValidationConstants.SerialNumberMaxLength);

        RuleFor(options => options.GetDepartmentNames())
            .NotEmpty()
            .Must(names => names.Distinct(StringComparer.Ordinal).Count() == names.Count)
            .WithName(nameof(DevelopmentDataOptions.DepartmentNames));

        RuleForEach(options => options.GetDepartmentNames())
            .NotEmpty()
            .MaximumLength(DepartmentValidationConstants.NameMaxLength)
            .WithName(nameof(DevelopmentDataOptions.DepartmentNames));

        RuleFor(options => options.GetTimeZoneIds())
            .NotEmpty()
            .WithName(nameof(DevelopmentDataOptions.TimeZoneIds));

        RuleForEach(options => options.GetTimeZoneIds())
            .NotEmpty()
            .MaximumLength(DepartmentValidationConstants.TimeZoneIdMaxLength)
            .Must(timeZoneId => TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
            .WithName(nameof(DevelopmentDataOptions.TimeZoneIds));

        RuleFor(options => options.GetMachineTypes())
            .NotEmpty()
            .WithName(nameof(DevelopmentDataOptions.MachineTypes));

        RuleForEach(options => options.GetMachineTypes())
            .NotEmpty()
            .MaximumLength(
                MachineValidationConstants.NameMaxLength - SeedingValidationConstants.MachineNameSuffixLength)
            .WithName(nameof(DevelopmentDataOptions.MachineTypes));
    }
}
