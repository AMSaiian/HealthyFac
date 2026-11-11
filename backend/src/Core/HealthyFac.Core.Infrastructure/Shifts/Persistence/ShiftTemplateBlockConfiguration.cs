using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Shifts.Persistence;

/// <summary>
/// Configures entity framework mapping for a work block of a template version.
/// </summary>
public class ShiftTemplateBlockConfiguration : IEntityTypeConfiguration<ShiftTemplateBlock>
{
    private const string EndAfterStartCheckName = "ck_shift_template_blocks_end_after_start";
    private const string EndAfterStartCheck = "\"end\" > start";

    public void Configure(EntityTypeBuilder<ShiftTemplateBlock> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(block => block.Weekday)
            .IsRequired();

        builder.HasOne<ShiftTemplateVersion>()
            .WithMany(version => version.Blocks)
            .HasForeignKey(block => block.ShiftTemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasEnumerationCheck(block => block.Weekday)
            .HasNotEmptyGuidCheck(block => block.ShiftTemplateVersionId);

        builder.ToTable(table => table.HasCheckConstraint(EndAfterStartCheckName, EndAfterStartCheck));
    }
}
