using FluentValidation;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats;

public record CreateChatRequest : ChatMessageInput, IRequest<ChatTurnResponse>
{
    public required Guid CompanyId { get; init; }
}

public sealed class CreateChatRequestValidator : ChatMessageInputValidator<CreateChatRequest>
{
    public CreateChatRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ChatErrors.CompanyIdRequired);
    }
}

public sealed class CreateChatPreHandlerCheck(ICurrentUserProvider currentUser) : IPreHandlerCheck<CreateChatRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(CreateChatRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Creates a chat with its first turn and runs the turn. One save stores the chat and the 2 messages before the turn
/// starts, so a turn that ends early leaves them stored. The title is the start of the text.
/// </summary>
/// <param name="dbContext">Stores the chat.</param>
/// <param name="currentUser">Provides the user of the actor.</param>
/// <param name="timeProvider">Provides the time of the turn.</param>
/// <param name="runner">Runs the turn.</param>
public sealed class CreateChatRequestHandler(
    IInsightsDbContext dbContext,
    ICurrentUserProvider currentUser,
    TimeProvider timeProvider,
    ChatTurnRunner runner
) : IRequestHandler<CreateChatRequest, ChatTurnResponse>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ChatTurnRunner _runner = runner;

    public async Task<ChatTurnResponse> HandleAsync(
        CreateChatRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var chat = new Chat(_currentUser.GetUserId(), request.CompanyId, now);
        var (user, assistant) = chat.AddTurn(
            request.Text,
            [
                .. request.Links.Select(link => new ChatMessageLink(
                    link.Kind, link.EntityId, string.Empty, link.Position)),
            ],
            now);
        _dbContext.Chats.Add(chat);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _runner.RunAsync(chat, user, assistant, cancellationToken);

        return chat.ToTurnResponse(user, assistant);
    }
}
