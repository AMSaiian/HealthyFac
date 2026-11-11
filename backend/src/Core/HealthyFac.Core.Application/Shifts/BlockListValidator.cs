using FluentValidation;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// Checks the blocks of one day for a template version and for an override: at most 6 blocks, each ending after it
/// starts, all times on the 5-minute grid, sorted by start, and without overlap. A block may start where the previous
/// one ends. An empty list passes (a day off).
/// </summary>
public sealed class BlockListValidator : AbstractValidator<IReadOnlyList<ShiftBlockInput>>
{
    public BlockListValidator()
    {
        RuleFor(blocks => blocks.Count)
            .LessThanOrEqualTo(ShiftValidationConstants.MaxBlocksPerDay)
            .WithMessage(ShiftErrors.Validation.TooManyBlocks);

        RuleFor(blocks => blocks)
            .Cascade(CascadeMode.Stop)
            .Must(blocks => blocks.All(block => block.End > block.Start))
            .WithMessage(ShiftErrors.Validation.BlockEndBeforeStart)
            .Must(blocks => blocks.All(IsOnGrid))
            .WithMessage(ShiftErrors.Validation.BlockOffGrid)
            .Must(IsSorted)
            .WithMessage(ShiftErrors.Validation.BlocksUnsorted)
            .Must(HasNoOverlap)
            .WithMessage(ShiftErrors.Validation.BlocksOverlap);
    }

    private static bool IsOnGrid(ShiftBlockInput block) =>
        block.Start.Ticks % SlotRules.GridStep.Ticks == 0 && block.End.Ticks % SlotRules.GridStep.Ticks == 0;

    private static bool IsSorted(IReadOnlyList<ShiftBlockInput> blocks) =>
        blocks.Zip(blocks.Skip(count: 1)).All(pair => pair.First.Start <= pair.Second.Start);

    private static bool HasNoOverlap(IReadOnlyList<ShiftBlockInput> blocks) =>
        blocks.Zip(blocks.Skip(count: 1)).All(pair => pair.Second.Start >= pair.First.End);
}
