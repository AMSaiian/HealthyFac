using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Chats.Persistence;

/// <summary>
/// Configures entity framework mapping for the chat message entity. A unique index of the chat and the sequence keeps
/// one place for each message. A message of the user has a text of at most <see cref="ChatLimits.TextMaxLength"/>
/// characters. The run of a reply stays when a message is removed (<c>Restrict</c>).
/// </summary>
public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    private const string UserTextRule = "user_text_length";

    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();

        builder.Property(message => message.Role).IsRequired();
        builder.Property(message => message.State).IsRequired();
        builder.Property(message => message.Text).IsRequired();

        builder.HasMany(message => message.Links)
            .WithOne()
            .HasForeignKey(link => link.ChatMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(message => message.Links)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(message => message.Drafts)
            .WithOne()
            .HasForeignKey(draft => draft.ChatMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(message => message.Drafts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<AgentRun>()
            .WithMany()
            .HasForeignKey(message => message.AgentRunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(message => new { message.ChatId, message.Sequence })
            .HasDatabaseName(ChatIndexNames.MessageSequence)
            .IsUnique()
            .HasFilter(SoftDeleteConstants.NotDeletedFilter);

        builder.HasIndex(message => message.AgentRunId)
            .HasDatabaseName(ChatIndexNames.MessageRun);

        builder.HasEnumerationCheck(message => message.Role)
            .HasEnumerationCheck(message => message.Kind)
            .HasEnumerationCheck(message => message.State)
            .HasEnumerationCheck(message => message.ErrorCode)
            .HasNotEmptyGuidCheck(message => message.ChatId)
            .HasRangeCheck(message => message.Sequence, min: 1, int.MaxValue);

        builder.ToTable(table => table.HasCheckConstraint(
            CheckConstraintSql.ConstraintName(
                nameof(ChatMessage).ToSnakeCase(), nameof(ChatMessage.Text).ToSnakeCase(), UserTextRule),
            $"role <> '{ChatMessageRole.User.Value}' OR (char_length(text) BETWEEN 1 AND {ChatLimits.TextMaxLength})"));
    }
}
