using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.DefinitionDrafts.Persistence;

/// <summary>
/// Configures the draft proposal table. One check constraint ties the fields to the source: each source needs its own
/// fields and no field of the other source.
/// </summary>
public class DraftProposalConfiguration : IEntityTypeConfiguration<DraftProposal>
{
    public void Configure(EntityTypeBuilder<DraftProposal> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(proposal => proposal.Source)
            .IsRequired();

        builder.HasIndex(proposal => new { proposal.CompanyId, proposal.AttemptId })
            .HasDatabaseName(DefinitionDraftIndexNames.ProposalAttempt);

        builder.HasIndex(proposal => new { proposal.CompanyId, proposal.ChatId })
            .HasDatabaseName(DefinitionDraftIndexNames.ProposalChat);

        builder.HasEnumerationCheck(proposal => proposal.Source)
            .HasNotEmptyGuidCheck(proposal => proposal.CompanyId);

        builder.ToTable(table =>
            table.HasCheckConstraint(
                DefinitionDraftCheckNames.ProposalSourceFields, DefinitionDraftCheckNames.ProposalSourceFieldsSql));
    }
}
