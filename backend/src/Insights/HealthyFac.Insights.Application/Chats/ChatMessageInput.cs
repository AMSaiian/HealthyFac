using FluentValidation;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Insights.Application.Chats;

/// <summary>
/// The text and the links of a new chat message. <see cref="CreateChatRequest"/> and
/// <see cref="SendChatMessageRequest"/> derive from it, so one validator checks both.
/// </summary>
public abstract record ChatMessageInput
{
    public required string Text { get; init; }

    public IReadOnlyList<ChatLinkInput> Links
    {
        get;
        init => field = value ?? [];
    } = [];
}

/// <summary>
/// A link of a new message. The label is empty until the send resolves it.
/// </summary>
public record ChatLinkInput
{
    public required ChatLinkKind Kind { get; init; }

    public required Guid EntityId { get; init; }

    /// <summary>The place of the link in the text.</summary>
    public int Position { get; init; }
}

public abstract class ChatMessageInputValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : ChatMessageInput
{
    protected ChatMessageInputValidator()
    {
        RuleFor(request => request.Text)
            .NotEmpty()
            .MultilineText()
            .MaximumLength(ChatLimits.TextMaxLength);

        RuleFor(request => request.Links)
            .Must(links => links.Count <= ChatLimits.LinksMaxCount)
            .WithMessage(ChatErrors.LinksTooMany);

        RuleFor(request => request.Links)
            .Must(links => links.Select(link => (link.Kind, link.EntityId)).Distinct().Count() == links.Count)
            .WithMessage(ChatErrors.LinksDuplicate);

        RuleForEach(request => request.Links)
            .ChildRules(link =>
            {
                link.RuleFor(item => item.EntityId).NotEmpty();
                link.RuleFor(item => item.Position).GreaterThanOrEqualTo(valueToCompare: 0);
            });
    }
}
