using FluentValidation;
using HealthyFac.Core.Application.Lookups;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines.Lookups;

public record LookupMachinesRequest : IRequest<LookupResponse>
{
    public required Guid CompanyId { get; init; }

    /// <summary>Part of the machine name. Without it the lookup answers the first machines by name.</summary>
    public string? Search { get; init; }

    /// <summary>
    /// The machines to name. With it, the lookup returns those machines of the company, also deleted ones, so an old
    /// row keeps its name, and ignores <see cref="Search"/> and the page limit.
    /// </summary>
    public IReadOnlyList<Guid>? Ids { get; init; }
}

public sealed class LookupMachinesRequestValidator : AbstractValidator<LookupMachinesRequest>
{
    public LookupMachinesRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(LookupErrors.CompanyIdRequired);

        RuleFor(request => request.Search)
            .MaximumLength(LookupValidationConstants.SearchMaxLength)
            .WithMessage(LookupErrors.SearchTooLong)
            .SingleLineText();

        RuleFor(request => request.Ids)
            .Must(ids => ids!.Count <= GridValidationConstants.MaxLimit)
            .WithMessage(LookupErrors.TooManyIds)
            .When(request => request.Ids is not null);
    }
}

public sealed class LookupMachinesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<LookupMachinesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(LookupMachinesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the ID and the name of the machines of the company for a picker or a filter, at most
/// <see cref="LookupValidationConstants.MaxItems"/>, sorted by name. A deleted machine is not offered.
/// </summary>
/// <param name="dbContext">Reads the machines.</param>
public sealed class LookupMachinesRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<LookupMachinesRequest, LookupResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<LookupResponse> HandleAsync(
        LookupMachinesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (request.Ids is not null)
        {
            var named = await _dbContext.Machines
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Where(machine => machine.CompanyId == request.CompanyId && request.Ids.Contains(machine.Id))
                .OrderBy(machine => machine.Name)
                .ThenBy(machine => machine.Id)
                .Select(machine => new LookupOption
                    { Id = machine.Id, DisplayName = machine.Name, IsDeleted = machine.DeletedAt != null })
                .ToListAsync(cancellationToken);

            return new LookupResponse { Items = named };
        }

        var query = _dbContext.Machines.Where(machine => machine.CompanyId == request.CompanyId);

        if (!string.IsNullOrEmpty(request.Search))
        {
            query = query.Where(machine => machine.Name.Contains(request.Search));
        }

        var items = await query
            .OrderBy(machine => machine.Name)
            .ThenBy(machine => machine.Id)
            .Take(LookupValidationConstants.MaxItems)
            .Select(machine => new LookupOption
                { Id = machine.Id, DisplayName = machine.Name, IsDeleted = machine.DeletedAt != null })
            .ToListAsync(cancellationToken);

        return new LookupResponse { Items = items };
    }
}
