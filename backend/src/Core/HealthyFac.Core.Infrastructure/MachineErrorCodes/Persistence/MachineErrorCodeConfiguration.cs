using HealthyFac.Core.Application.MachineErrorCodes;
using HealthyFac.Core.Domain.MachineErrorCodes;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Shared.Infrastructure.Persistence.SoftDelete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.MachineErrorCodes.Persistence;

/// <summary>
/// Configures entity framework mapping for the machine error code entity. The unique index on the machine and the
/// code is the only guard for the uniqueness of a code, because the use cases do not query for it.
/// </summary>
public class MachineErrorCodeConfiguration : IEntityTypeConfiguration<MachineErrorCode>
{
    public void Configure(EntityTypeBuilder<MachineErrorCode> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(errorCode => errorCode.Code)
            .HasMaxLength(MachineErrorCodeValidationConstants.CodeMaxLength)
            .IsRequired();

        builder.HasIndex(errorCode => new { errorCode.MachineId, errorCode.Code })
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasOne<Machine>()
            .WithMany()
            .HasForeignKey(errorCode => errorCode.MachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasNotBlankCheck(errorCode => errorCode.Code)
            .HasLengthCheck(
                errorCode => errorCode.Description,
                0,
                RichTextConstants.HtmlMaxLength
            )
            .HasNotEmptyGuidCheck(errorCode => errorCode.MachineId);
    }
}
