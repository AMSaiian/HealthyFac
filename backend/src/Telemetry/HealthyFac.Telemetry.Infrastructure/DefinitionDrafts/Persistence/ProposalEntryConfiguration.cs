using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

/// <summary>
/// Configures entity framework mapping for the proposal entry. The ID comes from the message, so the database never
/// generates it. A check constraint keeps the machine only on an entry in the state <c>onMachine</c>.
/// </summary>
public class ProposalEntryConfiguration : IEntityTypeConfiguration<ProposalEntry>
{
    public void Configure(EntityTypeBuilder<ProposalEntry> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.ConfigureVersioned();

        builder.Property(entry => entry.State)
            .IsRequired();

        builder.HasEnumerationCheck(entry => entry.State)
            .HasNotEmptyGuidCheck(entry => entry.CompanyId);

        builder.ToTable(table =>
            table.HasCheckConstraint(
                DefinitionDraftCheckNames.EntryMachine, DefinitionDraftCheckNames.EntryMachineSql));
    }
}
