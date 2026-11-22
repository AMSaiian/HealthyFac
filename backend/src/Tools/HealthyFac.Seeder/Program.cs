using HealthyFac.Seeder;

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stop.Cancel();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => stop.Cancel();

return await SeedProgram.RunAsync(args, Console.Out, AppContext.BaseDirectory, stop.Token);
