using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Chats;

public record SendChatMessageRequest : ChatMessageInput, IRequest<ChatTurnResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid ChatId { get; init; }
}

public sealed class SendChatMessageRequestValidator : ChatMessageInputValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ChatErrors.CompanyIdRequired);
        RuleFor(request => request.ChatId).NotEmpty().WithMessage(ChatErrors.ChatIdRequired);
    }
}

public sealed class SendChatMessagePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SendChatMessageRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SendChatMessageRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Adds a turn to a chat of the actor and runs it. A chat of another user is "not found". A blocked chat is
/// <c>422</c> (<see cref="ChatErrors.ChatBlocked"/>). A reply that streams for less than the turn limit is <c>409</c>
/// (<see cref="ChatErrors.TurnInProgress"/>). A reply that streams for longer has no process any more: the same save
/// that stores the new turn ends it <c>interrupted</c>. The save checks the row version of the chat, so a parallel
/// send gets <c>409</c> too. The unique sequence index of the messages catches a parallel send whose save reaches the
/// database first.
/// </summary>
/// <param name="dbContext">Loads and stores the chat.</param>
/// <param name="currentUser">Provides the user of the actor.</param>
/// <param name="timeProvider">Provides the time of the turn.</param>
/// <param name="options">Holds the turn limit.</param>
/// <param name="runner">Runs the turn.</param>
public sealed class SendChatMessageRequestHandler(
    IInsightsDbContext dbContext,
    ICurrentUserProvider currentUser,
    TimeProvider timeProvider,
    IOptions<ChatTurnOptions> options,
    ChatTurnRunner runner
) : IRequestHandler<SendChatMessageRequest, ChatTurnResponse>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ChatTurnOptions _options = options.Value;
    private readonly ChatTurnRunner _runner = runner;

    public async Task<ChatTurnResponse> HandleAsync(
        SendChatMessageRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var ownerId = _currentUser.GetUserId();

        var chat = await _dbContext.Chats
                .Include(candidate => candidate.Messages)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.ChatId
                        && candidate.OwnerUserId == ownerId
                        && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(ChatErrors.ChatNotFound);

        if (chat.State == ChatState.Blocked)
        {
            throw new UnprocessableException(ChatErrors.ChatBlocked);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var limit = TimeSpan.FromSeconds(_options.TurnLimitSeconds);
        foreach (var running in chat.Messages.Where(message => message.IsStreaming))
        {
            if (!running.IsDead(now, limit))
            {
                throw new ConflictException(ChatErrors.TurnInProgress);
            }

            running.End(ChatMessageState.Interrupted, errorCode: null);
        }

        var (user, assistant) = chat.AddTurn(
            request.Text,
            [
                .. request.Links.Select(link => new ChatMessageLink(
                    link.Kind, link.EntityId, string.Empty, link.Position)),
            ],
            now);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateConcurrencyException or UniqueConstraintException)
        {
            throw new ConflictException(ChatErrors.TurnConflict, exception);
        }

        await _runner.RunAsync(chat, user, assistant, cancellationToken);

        return chat.ToTurnResponse(user, assistant);
    }
}
