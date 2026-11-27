using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Insights.Infrastructure.Chats.Persistence;

/// <summary>
/// Configures entity framework mapping for the chat entity. The soft delete of a chat cascades to its messages. An
/// index of the owner, the company, and the last activity serves the list of the chats of a user.
/// </summary>
public class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.ConfigureEntity();
        builder.ConfigureAuditable();
        builder.ConfigureSoftDeletable();
        builder.ConfigureVersioned();

        builder.Property(chat => chat.Title)
            .HasMaxLength(ChatLimits.TitleMaxLength)
            .IsRequired();

        builder.Property(chat => chat.State)
            .IsRequired();

        builder.HasMany(chat => chat.Messages)
            .WithOne()
            .HasForeignKey(message => message.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(chat => chat.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(chat => new { chat.OwnerUserId, chat.CompanyId, chat.LastActivityAt })
            .HasDatabaseName(ChatIndexNames.OwnerList);

        builder.HasEnumerationCheck(chat => chat.State)
            .HasNotEmptyGuidCheck(chat => chat.OwnerUserId)
            .HasNotEmptyGuidCheck(chat => chat.CompanyId)
            .HasLengthCheck(chat => chat.Title, minLength: 0, ChatLimits.TitleMaxLength)
            .HasRangeCheck(chat => chat.NextSequence, min: 1, int.MaxValue);
    }
}
