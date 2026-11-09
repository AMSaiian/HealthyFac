namespace HealthyFac.Document.Infrastructure.Files.Persistence;

/// <summary>
/// Defines the database index names of the file ref table that code reads, so a name never differs between the
/// mapping and a check.
/// </summary>
public static class FileRefIndexNames
{
    public const string Scope = "ix_file_refs_company_id_machine_id_purpose";
}
