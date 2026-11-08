using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.Infrastructure.Intake.Persistence;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.Intake.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.Intake;

[Collection(TelemetryIntakeCollection.Name)]
public sealed class ReceivedMessageConstraintTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(
        nameof(ReceivedMessageConstraintTestData.InvalidPayloadForms),
        MemberType = typeof(ReceivedMessageConstraintTestData))]
    public async Task Insert_WithBothOrNeitherPayloadForm_IsRejectedByTheCheckConstraint(string sql)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

        var act = () => dbContext.Database.ExecuteSqlRawAsync(sql);

        await act.Should()
            .ThrowAsync<Exception>()
            .Where(exception => exception.ToString()
                .Contains(
                    ReceivedMessageConstants.ExactlyOnePayloadFormConstraintName));
    }
}
