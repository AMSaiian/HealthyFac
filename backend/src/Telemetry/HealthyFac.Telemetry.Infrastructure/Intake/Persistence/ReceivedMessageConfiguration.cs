using System.Text.Json;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Telemetry.Infrastructure.Intake.Persistence;

/// <summary>
/// Configures entity framework mapping for the received message entity. The payload and the errors are
/// <c>jsonb</c>. The unique index on the machine and the sampling time covers the rows that have both values and drops
/// a repeated delivery.
/// </summary>
public class ReceivedMessageConfiguration : IEntityTypeConfiguration<ReceivedMessage>
{
    private const string JsonbColumnType = "jsonb";
    private const string ByteaColumnType = "bytea";

    public void Configure(EntityTypeBuilder<ReceivedMessage> builder)
    {
        builder.ConfigureEntity();

        builder.Property(message => message.Topic)
            .HasMaxLength(ReceivedMessageConstants.TopicMaxLength)
            .IsRequired();

        builder.Property(message => message.RawPayload)
            .HasColumnType(ByteaColumnType);

        builder.Property(message => message.Payload)
            .HasColumnType(JsonbColumnType);

        builder.Property(message => message.Errors)
            .HasColumnType(JsonbColumnType)
            .HasConversion(
                errors => JsonSerializer.Serialize(errors, JsonSerializerOptions.Web),
                json => JsonSerializer.Deserialize<List<IntakeError>>(json, JsonSerializerOptions.Web)!,
                new ValueComparer<IReadOnlyList<IntakeError>>(
                    (left, right) => left!.SequenceEqual(right!),
                    errors => errors.Aggregate(seed: 0, (hash, error) => HashCode.Combine(hash, error)),
                    errors => errors.ToList()))
            .IsRequired();

        builder.HasIndex(message => new { message.MachineRegistrationId, message.SampledAt })
            .HasDatabaseName(ReceivedMessageConstants.IndexNames.MachineSampledAt)
            .IsUnique()
            .HasFilter(ReceivedMessageConstants.UniqueFilter);

        builder.ToTable(table => table.HasCheckConstraint(
            ReceivedMessageConstants.ExactlyOnePayloadFormConstraintName,
            ReceivedMessageConstants.ExactlyOnePayloadFormCheck));

        builder.HasOne<MachineRegistration>()
            .WithMany()
            .HasForeignKey(message => message.MachineRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
