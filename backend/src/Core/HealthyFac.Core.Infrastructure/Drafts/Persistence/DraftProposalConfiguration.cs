using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Core.Infrastructure.Drafts.Persistence;

/// <summary>
/// Configures the draft proposal table. One check constraint ties the fields to the source: each source needs its own
/// fields and no field of another source.
/// </summary>
public class DraftProposalConfiguration : IEntityTypeConfiguration<DraftProposal>
{
    public const string SourceFieldsCheckName = "ck_draft_proposals_source_fields";

    private static readonly string SourceFieldsCheck =
        $"""
         (source = '{DraftSource.Ingest.Value}'
             AND attempt_id IS NOT NULL AND file_id IS NOT NULL AND queued_at IS NOT NULL
             AND case_id IS NULL AND run_id IS NULL AND chat_id IS NULL AND chat_message_id IS NULL)
         OR (source = '{DraftSource.Case.Value}'
             AND case_id IS NOT NULL AND run_id IS NOT NULL
             AND attempt_id IS NULL AND file_id IS NULL AND queued_at IS NULL
             AND chat_id IS NULL AND chat_message_id IS NULL)
         OR (source = '{DraftSource.Chat.Value}'
             AND chat_id IS NOT NULL AND chat_message_id IS NOT NULL
             AND attempt_id IS NULL AND file_id IS NULL AND queued_at IS NULL
             AND case_id IS NULL AND run_id IS NULL)
         """;

    public void Configure(EntityTypeBuilder<DraftProposal> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(proposal => proposal.Source)
            .IsRequired();

        builder.HasIndex(proposal => new { proposal.CompanyId, proposal.AttemptId })
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter)
            .HasDatabaseName(DraftIndexNames.ProposalAttempt);

        builder.HasIndex(proposal => proposal.RunId)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter)
            .HasDatabaseName(DraftIndexNames.ProposalRun);

        builder.HasIndex(proposal => proposal.ChatMessageId)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter)
            .HasDatabaseName(DraftIndexNames.ProposalChatMessage);

        builder.HasIndex(proposal => new { proposal.CompanyId, proposal.ChatId })
            .HasDatabaseName(DraftIndexNames.ProposalChat);

        builder.HasEnumerationCheck(proposal => proposal.Source)
            .HasNotEmptyGuidCheck(proposal => proposal.CompanyId);

        builder.ToTable(table => table.HasCheckConstraint(SourceFieldsCheckName, SourceFieldsCheck));
    }
}
