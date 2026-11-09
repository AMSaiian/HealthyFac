using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Document.Domain.Files;

/// <summary>
/// Defines what a file is for. The camelCase values reach the database, so never rename one in place.
/// </summary>
public sealed class FilePurpose : Enumeration<FilePurpose>
{
    /// <summary>A file that a user attaches to the company or to a machine. It shows in the file grids.</summary>
    public static readonly FilePurpose Attachment = new(value: "attachment");

    /// <summary>An image inside a rich text description. It belongs to the company and shows in no grid.</summary>
    public static readonly FilePurpose Embedded = new(value: "embedded");

    private FilePurpose(string value)
        : base(value)
    {
    }
}
