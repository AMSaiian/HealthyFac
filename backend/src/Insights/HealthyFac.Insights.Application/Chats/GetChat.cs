using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Chats;

public record GetChatRequest : IRequest<ChatResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid ChatId { get; init; }
}

public sealed class GetChatRequestValidator : AbstractValidator<GetChatRequest>
{
    public GetChatRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ChatErrors.CompanyIdRequired);
        RuleFor(request => request.ChatId).NotEmpty().WithMessage(ChatErrors.ChatIdRequired);
    }
}

public sealed class GetChatPreHandlerCheck(ICurrentUserProvider currentUser) : IPreHandlerCheck<GetChatRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetChatRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the header of one chat. A chat of another user, a chat of another company, and a deleted chat are "not
/// found", so the answer does not show that the chat exists.
/// </summary>
/// <param name="dbContext">Reads the chat.</param>
/// <param name="currentUser">Provides the user of the actor.</param>
public sealed class GetChatRequestHandler(IInsightsDbContext dbContext, ICurrentUserProvider currentUser)
    : IRequestHandler<GetChatRequest, ChatResponse>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<ChatResponse> HandleAsync(
        GetChatRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var ownerId = _currentUser.GetUserId();

        var chat = await _dbContext.Chats
                .AsNoTracking()
                .Where(candidate => candidate.Id == request.ChatId
                    && candidate.OwnerUserId == ownerId
                    && candidate.CompanyId == request.CompanyId)
                .Select(candidate => new ChatResponse
                {
                    Id = candidate.Id,
                    Title = candidate.Title,
                    State = candidate.State,
                    MessageCount = candidate.Messages.Count,
                })
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(ChatErrors.ChatNotFound);

        return chat;
    }
}
