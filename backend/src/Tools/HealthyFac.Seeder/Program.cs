using System.Runtime.InteropServices;
using HealthyFac.Seeder;

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stop.Cancel();
};
using var termination = PosixSignalRegistration.Create(
    PosixSignal.SIGTERM,
    context =>
    {
        context.Cancel = true;
        stop.Cancel();
    });

return await SeedProgram.RunAsync(args, Console.Out, AppContext.BaseDirectory, stop.Token);
