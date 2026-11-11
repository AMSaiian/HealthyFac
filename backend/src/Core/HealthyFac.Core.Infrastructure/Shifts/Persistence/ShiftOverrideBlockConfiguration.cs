using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Shifts.Persistence;

/// <summary>
/// Configures entity framework mapping for a work block of a shift override.
/// </summary>
public class ShiftOverrideBlockConfiguration : IEntityTypeConfiguration<ShiftOverrideBlock>
{
    private const string EndAfterStartCheckName = "ck_shift_override_blocks_end_after_start";
    private const string EndAfterStartCheck = "\"end\" > start";

    public void Configure(EntityTypeBuilder<ShiftOverrideBlock> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.HasOne<ShiftOverride>()
            .WithMany(shiftOverride => shiftOverride.Blocks)
            .HasForeignKey(block => block.ShiftOverrideId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasNotEmptyGuidCheck(block => block.ShiftOverrideId);

        builder.ToTable(table => table.HasCheckConstraint(EndAfterStartCheckName, EndAfterStartCheck));
    }
}
