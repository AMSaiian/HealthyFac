using HealthyFac.Document.Application.Files.Inspection;
using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.ApiTests.Files.Data;

/// <summary>
/// An inspector that holds every call until the test opens the gate, so the test keeps uploads in flight.
/// </summary>
public sealed class GatedFileInspector : IFileInspector
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _enteredCount;

    public int EnteredCount => Volatile.Read(ref _enteredCount);

    public void Open() => _gate.TrySetResult();

    public async Task<FileInspection> InspectAsync(
        Stream seekableContent,
        string? clientExtension,
        string declaredContentType,
        FilePurpose purpose,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Interlocked.Increment(ref _enteredCount);
        await _gate.Task.WaitAsync(cancellationToken);

        return new FileInspection { Type = null, SizeBytes = seekableContent.Length, Sha256 = new byte[32] };
    }
}
