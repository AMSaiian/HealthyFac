using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Chats.Persistence;

/// <summary>
/// Configures entity framework mapping for the link of a chat message.
/// </summary>
public class ChatMessageLinkConfiguration : IEntityTypeConfiguration<ChatMessageLink>
{
    public void Configure(EntityTypeBuilder<ChatMessageLink> builder)
    {
        builder.ToTable(name: "chat_message_links");

        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(link => link.Kind).IsRequired();
        builder.Property(link => link.Label).HasMaxLength(ChatMessageLink.LabelMaxLength).IsRequired();

        builder.HasEnumerationCheck(link => link.Kind)
            .HasNotBlankCheck(link => link.Label)
            .HasNotEmptyGuidCheck(link => link.ChatMessageId)
            .HasNotEmptyGuidCheck(link => link.EntityId)
            .HasRangeCheck(link => link.Position, min: 0, int.MaxValue);
    }
}
