using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Chats.Persistence;

/// <summary>
/// Configures entity framework mapping for the draft link of a chat message.
/// </summary>
public class ChatMessageDraftConfiguration : IEntityTypeConfiguration<ChatMessageDraft>
{
    private const int TitleMaxLength = 255;

    public void Configure(EntityTypeBuilder<ChatMessageDraft> builder)
    {
        builder.ToTable(name: "chat_message_drafts");

        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(draft => draft.Kind).IsRequired();
        builder.Property(draft => draft.Title).HasMaxLength(TitleMaxLength).IsRequired();

        builder.HasEnumerationCheck(draft => draft.Kind)
            .HasNotEmptyGuidCheck(draft => draft.ChatMessageId)
            .HasNotEmptyGuidCheck(draft => draft.DraftId)
            .HasNotBlankCheck(draft => draft.Title);
    }
}
