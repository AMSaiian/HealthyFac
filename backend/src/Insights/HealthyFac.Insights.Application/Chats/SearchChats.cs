using System.Collections.Frozen;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Chats;

/// <summary>The columns that the chat list sorts by.</summary>
public sealed class ChatSortColumn : Enumeration<ChatSortColumn>
{
    public static readonly ChatSortColumn UpdatedAt = new(value: "updatedAt");

    private ChatSortColumn(string value)
        : base(value)
    {
    }
}

/// <summary>The chat list has no filter. The user sees only the own chats.</summary>
public sealed record ChatGridFilter;

/// <summary>The sort map of the chat list. The latest activity comes first by default.</summary>
public static class ChatSortMap
{
    public static readonly FrozenDictionary<ChatSortColumn, IKeysetSortKey<ChatListItem>> Instance =
        new Dictionary<ChatSortColumn, IKeysetSortKey<ChatListItem>>
        {
            [ChatSortColumn.UpdatedAt] = KeysetSortKey.For((ChatListItem item) => item.UpdatedAt),
        }.ToFrozenDictionary();

    public static readonly GridSort<ChatSortColumn> Default = new()
    {
        Column = ChatSortColumn.UpdatedAt,
        Direction = SortDirection.Descending,
    };
}

public record SearchChatsRequest : KeysetGridRequest<ChatGridFilter, ChatSortColumn>,
    IRequest<KeysetGridResponse<ChatListItem>>
{
    public required Guid CompanyId { get; init; }
}

public sealed class SearchChatsRequestValidator
    : KeysetGridRequestValidator<SearchChatsRequest, ChatGridFilter, ChatSortColumn>
{
    public SearchChatsRequestValidator()
        : base(ChatSortColumn.All)
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ChatErrors.CompanyIdRequired);
    }
}

public sealed class SearchChatsPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchChatsRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SearchChatsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page of the chats of the actor in the company, the latest activity first. A chat of another user is
/// never in the page.
/// </summary>
/// <param name="dbContext">Reads the chats.</param>
/// <param name="currentUser">Provides the user of the actor.</param>
public sealed class SearchChatsRequestHandler(IInsightsDbContext dbContext, ICurrentUserProvider currentUser)
    : IRequestHandler<SearchChatsRequest, KeysetGridResponse<ChatListItem>>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<KeysetGridResponse<ChatListItem>> HandleAsync(
        SearchChatsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var ownerId = _currentUser.GetUserId();

        var response = await _dbContext.Chats
            .AsNoTracking()
            .Where(chat => chat.OwnerUserId == ownerId && chat.CompanyId == request.CompanyId)
            .Select(chat => new ChatListItem
            {
                Id = chat.Id,
                Title = chat.Title,
                State = chat.State,
                UpdatedAt = chat.LastActivityAt,
            })
            .ToKeysetResponseAsync(
                request.Sort ?? ChatSortMap.Default,
                ChatSortMap.Instance,
                request.Limit,
                request.After,
                cancellationToken);

        return response;
    }
}
