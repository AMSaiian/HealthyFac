using FluentValidation;
using FluentValidation.Results;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Chats;

public record DeleteChatMessageRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid ChatId { get; init; }

    public required Guid MessageId { get; init; }
}

public sealed class DeleteChatMessageRequestValidator : AbstractValidator<DeleteChatMessageRequest>
{
    public DeleteChatMessageRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ChatErrors.CompanyIdRequired);
        RuleFor(request => request.ChatId).NotEmpty().WithMessage(ChatErrors.ChatIdRequired);
        RuleFor(request => request.MessageId).NotEmpty().WithMessage(ChatErrors.MessageIdRequired);
    }
}

public sealed class DeleteChatMessagePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DeleteChatMessageRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(DeleteChatMessageRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Deletes a message of the user and the reply to it. The summary becomes stale when it covers the message. A chat of
/// another user and an unknown message are "not found". The ID of a reply is a bad request, and a reply that still
/// runs is a conflict (<see cref="ChatErrors.TurnInProgress"/>). Every delete changes the chat row (its last activity),
/// so the save checks the row version of the chat: a turn or a compaction that ran at the same time makes the delete
/// fail with a conflict (<see cref="ChatErrors.ChatChanged"/>) instead of losing the change.
/// </summary>
/// <param name="dbContext">Loads the chat and removes the messages.</param>
/// <param name="currentUser">Provides the user of the actor.</param>
/// <param name="timeProvider">Provides the time of the delete.</param>
public sealed class DeleteChatMessageRequestHandler(
    IInsightsDbContext dbContext,
    ICurrentUserProvider currentUser,
    TimeProvider timeProvider
) : RequestHandler<DeleteChatMessageRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        DeleteChatMessageRequest request,
        CancellationToken cancellationToken
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

        var message = chat.Messages.SingleOrDefault(candidate => candidate.Id == request.MessageId)
            ?? throw new NotFoundException(ChatErrors.MessageNotFound);

        if (message.Role != ChatMessageRole.User)
        {
            throw new AppValidationException(
                [new ValidationFailure(nameof(DeleteChatMessageRequest.MessageId), ChatErrors.NotAUserMessage)]);
        }

        var isReplyRunning = chat.Messages.Any(candidate =>
            candidate.Sequence == message.Sequence + 1 && candidate.IsStreaming);

        if (isReplyRunning)
        {
            throw new ConflictException(ChatErrors.TurnInProgress);
        }

        var removed = chat.DeleteUserMessage(message.Id, _timeProvider.GetUtcNow().UtcDateTime);
        _dbContext.ChatMessages.RemoveRange(removed);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(ChatErrors.ChatChanged, exception);
        }
    }
}
