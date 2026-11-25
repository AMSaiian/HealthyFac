using System.Text;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// Collects the text of a running turn and pushes the full text to the chat owner at most once per interval. The
/// first piece goes out at once. <see cref="Flush"/> pushes the text that is still pending.
/// </summary>
public sealed class ChatSnapshotThrottle
{
    private readonly IRealtimeNotifier _notifier;
    private readonly TimeProvider _timeProvider;
    private readonly StringBuilder _text = new();
    private readonly TimeSpan _interval;
    private readonly Guid _ownerUserId;
    private readonly Guid _chatId;
    private readonly Guid _messageId;

    /// <param name="notifier">Pushes the snapshot.</param>
    /// <param name="timeProvider">The clock of the interval.</param>
    /// <param name="interval">The shortest time between 2 pushes.</param>
    /// <param name="ownerUserId">The user who gets the pushes.</param>
    /// <param name="chatId">The chat of the turn.</param>
    /// <param name="messageId">The reply that streams.</param>
    public ChatSnapshotThrottle(
        IRealtimeNotifier notifier,
        TimeProvider timeProvider,
        TimeSpan interval,
        Guid ownerUserId,
        Guid chatId,
        Guid messageId
    )
    {
        _notifier = notifier;
        _timeProvider = timeProvider;
        _interval = interval;
        _ownerUserId = ownerUserId;
        _chatId = chatId;
        _messageId = messageId;
    }

    private long? _lastPushTimestamp;
    private int _sequence;
    private bool _isPending;

    /// <summary>The full text that the throttle collected.</summary>
    public string Text => _text.ToString();

    /// <summary>
    /// Adds a piece of text and pushes the snapshot when the interval has passed since the last push.
    /// </summary>
    /// <param name="piece">The new text.</param>
    public void Append(string piece)
    {
        if (piece.Length == 0)
        {
            return;
        }

        _text.Append(piece);
        _isPending = true;
        if (_lastPushTimestamp is null || _timeProvider.GetElapsedTime(_lastPushTimestamp.Value) >= _interval)
        {
            Push();
        }
    }

    /// <summary>
    /// Pushes the snapshot when text is pending.
    /// </summary>
    public void Flush()
    {
        if (_isPending)
        {
            Push();
        }
    }

    private void Push()
    {
        _sequence++;
        _lastPushTimestamp = _timeProvider.GetTimestamp();
        _isPending = false;
        _notifier.AddChatTurnUpdate(
            _ownerUserId,
            new ChatTurnUpdate
            {
                ChatId = _chatId,
                MessageId = _messageId,
                Sequence = _sequence,
                Text = _text.ToString(),
            });
        _notifier.Push();
    }
}
