namespace HealthyFac.Document.Domain.Files;

/// <summary>
/// Defines the exceptions that a file ref throws when a caller breaks an invariant. The Application layer checks the
/// same rules first, so these exceptions mark a programming error.
/// </summary>
public static class FileInvariantErrors
{
    public static InvalidOperationException EmbeddedFileWithMachine() =>
        new(message: "An embedded image belongs to the company and has no machine.");

    public static InvalidOperationException EmptyFile() => new(message: "A file must have at least 1 byte.");

    public static InvalidOperationException InvalidHash(int expectedLength) =>
        new($"The SHA-256 hash must have {expectedLength} bytes.");
}
