namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// Marks a string property of a request or a message as an opaque value (e.g., a token or a signature). The input
/// stays unchanged. Only the null character is rejected, because PostgreSQL cannot store it.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RawTextAttribute : Attribute;
