using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Document.Infrastructure.Files.Persistence;

/// <summary>
/// Configures entity framework mapping for the file ref entity. The row version (<c>xmin</c>) makes two parallel
/// renames conflict. The checks mirror the invariants of the entity, so a row that bypasses the entity still fails.
/// </summary>
public class FileRefConfiguration : IEntityTypeConfiguration<FileRef>
{
    private const int ExtensionMaxLength = 8;
    private const int ContentTypeMaxLength = 127;
    private const string AllowedRule = "allowed";
    private const string EmbeddedRule = "embedded_without_machine";
    private const string HashRule = "hash_length";

    public void Configure(EntityTypeBuilder<FileRef> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(file => file.FileName)
            .HasMaxLength(FileValidationConstants.Text.FileNameMaxLength)
            .IsRequired();

        builder.Property(file => file.Extension)
            .HasMaxLength(ExtensionMaxLength)
            .IsRequired();

        builder.Property(file => file.ContentType)
            .HasMaxLength(ContentTypeMaxLength)
            .IsRequired();

        builder.Property(file => file.Purpose)
            .IsRequired();

        builder.Property(file => file.Sha256)
            .IsRequired();

        builder.Property(file => file.IngestFailedCount)
            .HasDefaultValue(value: 0);

        builder.ConfigureVersioned();

        builder.HasOne(file => file.Machine)
            .WithMany()
            .HasForeignKey(file => file.MachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(file => new { file.CompanyId, file.MachineId, file.Purpose })
            .HasDatabaseName(FileRefIndexNames.Scope)
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasEnumerationCheck(file => file.Purpose)
            .HasEnumerationCheck(file => file.IngestState)
            .HasRangeCheck(file => file.IngestFailedCount, min: 0, int.MaxValue)
            .HasNotBlankCheck(file => file.FileName)
            .HasLengthCheck(file => file.FileName, minLength: 1, FileValidationConstants.Text.FileNameMaxLength)
            .HasNotBlankCheck(file => file.ContentType)
            .HasLengthCheck(file => file.Description, minLength: 0, FileValidationConstants.Text.DescriptionMaxLength)
            .HasRangeCheck(file => file.SizeBytes, min: 1L, long.MaxValue)
            .HasNotEmptyGuidCheck(file => file.CompanyId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                ConstraintName(nameof(FileRef.Extension), AllowedRule),
                CheckConstraintSql.AllowedValues(
                    nameof(FileRef.Extension).ToSnakeCase(),
                    AllowedFileTypes.All.Select(type => type.Extension)));
            table.HasCheckConstraint(
                ConstraintName(nameof(FileRef.MachineId), EmbeddedRule),
                $"purpose <> '{FilePurpose.Embedded.Value}' OR machine_id IS NULL");
            table.HasCheckConstraint(
                ConstraintName(nameof(FileRef.Sha256), HashRule),
                $"octet_length(sha256) = {FileRef.Sha256Length}");
        });
    }

    private static string ConstraintName(string property, string rule) =>
        CheckConstraintSql.ConstraintName(nameof(FileRef).ToSnakeCase(), property.ToSnakeCase(), rule);
}
