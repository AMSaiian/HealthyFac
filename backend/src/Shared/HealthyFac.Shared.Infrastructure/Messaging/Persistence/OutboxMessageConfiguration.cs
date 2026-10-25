using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Shared.Infrastructure.Messaging.Persistence;

/// <summary>
/// Configures entity framework mapping for the outbox message.
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .ValueGeneratedNever();

        builder.Property(message => message.Type)
            .HasMaxLength(MessagingPersistenceConstants.TypeMaxLength)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnType(MessagingPersistenceConstants.PayloadColumnType)
            .IsRequired();

        builder.Property<uint>(PersistenceConstants.ConcurrencyToken)
            .IsRowVersion();

        builder.HasIndex(message => message.NextAttemptOn)
            .HasFilter(MessagingPersistenceConstants.PendingFilter);
    }
}
