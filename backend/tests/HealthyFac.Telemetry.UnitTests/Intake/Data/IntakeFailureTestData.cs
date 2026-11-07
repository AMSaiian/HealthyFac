using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HealthyFac.Telemetry.UnitTests.Intake.Data;

public static class IntakeFailureTestData
{
    public static TheoryData<Exception> TransientFailures => new()
    {
        new TimeoutException(),
        new NpgsqlException(message: "connection lost", new SocketException()),
        new DbUpdateException(message: "save failed", new TimeoutException()),
        new DbUpdateException(message: "save failed", new NpgsqlException(message: "x", new SocketException())),
    };

    public static TheoryData<Exception> PermanentFailures => new()
    {
        new InvalidOperationException(),
        new ArgumentException(message: "bad"),
        new DbUpdateException(message: "save failed", new InvalidOperationException()),
        new DbUpdateException(message: "save failed"),
    };
}
