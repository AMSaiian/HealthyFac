namespace HealthyFac.Seeder.SeedFiles;

/// <summary>A seed file that is missing or does not match its type. The message names the file and path.</summary>
public sealed class SeedFileException : Exception
{
    public SeedFileException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
