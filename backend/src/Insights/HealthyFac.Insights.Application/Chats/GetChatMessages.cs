using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Chats;

public record GetChatMessagesRequest : IRequest<ChatMessagesResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid ChatId { get; init; }

    public required int Limit { get; init; }

    /// <summary>
    /// The sequence of the oldest message of the previous page, or <see langword="null"/> for the first page. The
    /// page holds the messages with a lower sequence.
    /// </summary>
    public int? Before { get; init; }
}

public sealed class GetChatMessagesRequestValidator : AbstractValidator<GetChatMessagesRequest>
{
    public GetChatMessagesRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ChatErrors.CompanyIdRequired);
        RuleFor(request => request.ChatId).NotEmpty().WithMessage(ChatErrors.ChatIdRequired);

        RuleFor(request => request.Limit)
            .InclusiveBetween(GridValidationConstants.MinLimit, GridValidationConstants.MaxLimit)
            .WithMessage(GridErrors.Validation.LimitOutOfRange);

        RuleFor(request => request.Before!.Value)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(ChatErrors.BeforeInvalid)
            .When(request => request.Before is not null);
    }
}

public sealed class GetChatMessagesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetChatMessagesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetChatMessagesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page of the messages of a chat, the newest first, with their links and draft links. The read holds the
/// scope: a chat of another user and a deleted chat are "not found". A deleted message is left out.
/// </summary>
/// <param name="dbContext">Reads the chat and its messages.</param>
/// <param name="currentUser">Provides the user of the actor.</param>
public sealed class GetChatMessagesRequestHandler(IInsightsDbContext dbContext, ICurrentUserProvider currentUser)
    : IRequestHandler<GetChatMessagesRequest, ChatMessagesResponse>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<ChatMessagesResponse> HandleAsync(
        GetChatMessagesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var ownerId = _currentUser.GetUserId();

        var isOwned = await _dbContext.Chats
            .AsNoTracking()
            .AnyAsync(
                chat => chat.Id == request.ChatId && chat.OwnerUserId == ownerId && chat.CompanyId == request.CompanyId,
                cancellationToken);

        if (!isOwned)
        {
            throw new NotFoundException(ChatErrors.ChatNotFound);
        }

        var query = _dbContext.ChatMessages.AsNoTracking().Where(message => message.ChatId == request.ChatId);

        if (request.Before is { } before)
        {
            query = query.Where(message => message.Sequence < before);
        }

        var rows = await query
            .OrderByDescending(message => message.Sequence)
            .Take(request.Limit + 1)
            .Select(ChatMessageMapping.ToResponseExpression)
            .ToListAsync(cancellationToken);

        var items = rows.Take(request.Limit).ToList();

        return new ChatMessagesResponse
        {
            Items = items,
            NextBefore = rows.Count > request.Limit ? items[^1].Sequence : null,
        };
    }
}
