using FluentValidation;

namespace HealthyFac.Document.Web.Files;

/// <summary>
/// Configures how many uploads run at the same time. An upload buffers its body on disk and inspects it, so the limit
/// protects the disk and the CPU of the instance.
/// </summary>
public sealed class UploadLimits
{
    public const string SectionName = "UploadLimits";

    /// <summary>The name of the rate limiter policy that the upload endpoints use.</summary>
    public const string PolicyName = "file-upload";

    public const int PermitLimitMin = 1;
    public const int PermitLimitMax = 64;
    public const int QueueLimitMin = 0;
    public const int QueueLimitMax = 256;
    public const int MemoryBufferThresholdBytes = 65536;
    public const int RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    /// <summary>The number of uploads that run at the same time.</summary>
    public int PermitLimit { get; init; } = 4;

    /// <summary>The number of uploads that wait for a free place. A further upload gets status 429.</summary>
    public int QueueLimit { get; init; } = 8;
}

public sealed class UploadLimitsValidator : AbstractValidator<UploadLimits>
{
    public UploadLimitsValidator()
    {
        RuleFor(limits => limits.PermitLimit)
            .InclusiveBetween(UploadLimits.PermitLimitMin, UploadLimits.PermitLimitMax);

        RuleFor(limits => limits.QueueLimit)
            .InclusiveBetween(UploadLimits.QueueLimitMin, UploadLimits.QueueLimitMax);
    }
}
