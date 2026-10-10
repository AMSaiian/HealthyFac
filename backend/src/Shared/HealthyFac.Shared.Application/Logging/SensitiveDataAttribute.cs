namespace HealthyFac.Shared.Application.Logging;

/// <summary>
/// Marks a property, field, or class as containing sensitive information that requires masking in logs.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = true
)]
public class SensitiveDataAttribute : Attribute
{
    public string Mask { get; init; } = MaskingConstants.DefaultMask;

    public int ShowFirst { get; init; }

    public int ShowLast { get; init; }

    public bool PreserveLength { get; init; }

    public SensitiveDataAttribute()
    {
    }

    public SensitiveDataAttribute(string mask)
    {
        Mask = mask;
    }
}
