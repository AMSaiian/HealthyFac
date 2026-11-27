using FluentValidation;

namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// Holds the settings of a chat turn. The values come from <c>appsettings.json</c>; the defaults are the values of
/// the spec.
/// </summary>
public sealed class ChatTurnOptions
{
    public const string SectionName = "Insights:Chats";

    /// <summary>The time in seconds that a turn may run. The same value makes a streaming reply dead.</summary>
    public int TurnLimitSeconds { get; init; } = 90;

    /// <summary>The most tool calls of one turn.</summary>
    public int MaxToolCalls { get; init; } = 20;

    /// <summary>The shortest time in milliseconds between 2 snapshot pushes of one turn.</summary>
    public int PushIntervalMilliseconds { get; init; } = 150;
}

public sealed class ChatTurnOptionsValidator : AbstractValidator<ChatTurnOptions>
{
    public const string ValueNotPositive = "Every Insights:Chats value must be above 0.";

    public ChatTurnOptionsValidator()
    {
        RuleFor(options => options.TurnLimitSeconds).GreaterThan(valueToCompare: 0).WithMessage(ValueNotPositive);
        RuleFor(options => options.MaxToolCalls).GreaterThan(valueToCompare: 0).WithMessage(ValueNotPositive);

        RuleFor(options => options.PushIntervalMilliseconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(ValueNotPositive);
    }
}
