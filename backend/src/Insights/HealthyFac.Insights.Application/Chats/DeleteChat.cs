using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Chats;

public record DeleteChatRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid ChatId { get; init; }
}

public sealed class DeleteChatRequestValidator : AbstractValidator<DeleteChatRequest>
{
    public DeleteChatRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ChatErrors.CompanyIdRequired);
        RuleFor(request => request.ChatId).NotEmpty().WithMessage(ChatErrors.ChatIdRequired);
    }
}

public sealed class DeleteChatPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DeleteChatRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(DeleteChatRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Deletes a chat of the actor. The soft delete interceptor turns the removal into an update and soft-deletes the
/// messages, the links, and the draft links of the chat in the same save. A chat of another user and a deleted chat
/// are "not found".
/// </summary>
/// <param name="dbContext">Loads and removes the chat.</param>
/// <param name="currentUser">Provides the user of the actor.</param>
public sealed class DeleteChatRequestHandler(IInsightsDbContext dbContext, ICurrentUserProvider currentUser)
    : RequestHandler<DeleteChatRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    protected override async Task HandleInternalAsync(DeleteChatRequest request, CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.GetUserId();

        var chat = await _dbContext.Chats
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.ChatId
                        && candidate.OwnerUserId == ownerId
                        && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(ChatErrors.ChatNotFound);

        _dbContext.Chats.Remove(chat);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
