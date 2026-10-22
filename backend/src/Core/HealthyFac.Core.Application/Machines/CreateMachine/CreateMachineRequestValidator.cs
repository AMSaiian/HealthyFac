using FluentValidation;

namespace HealthyFac.Core.Application.Machines.CreateMachine;

public class CreateMachineRequestValidator : AbstractValidator<CreateMachineRequest>
{
    public CreateMachineRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(MachineErrors.NameRequired)
            .MaximumLength(MachineValidationConstants.NameMaxLength);

        RuleFor(x => x.SerialNumber)
            .NotEmpty()
            .WithMessage(MachineErrors.SerialNumberRequired)
            .MaximumLength(MachineValidationConstants.SerialNumberMaxLength);

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrors.CompanyIdRequired);

        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage(MachineErrors.DepartmentIdRequired);
    }
}
