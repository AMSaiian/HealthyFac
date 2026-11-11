namespace HealthyFac.Insights.Application.Conversion;

/// <summary>
/// Reports that the conversion service accepted a file and then failed to convert it.
/// </summary>
public sealed class ConversionFailedException : Exception
{
    public ConversionFailedException()
        : base()
    {
    }

    public ConversionFailedException(string message)
        : base(message)
    {
    }

    public ConversionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
