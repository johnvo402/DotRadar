using DotRadar.Tool;

using var cancellationSource =
    new CancellationTokenSource();

ConsoleCancelEventHandler cancelHandler =
    (_, eventArgs) =>
    {
        // Ctrl+C lần đầu yêu cầu graceful cancellation.
        if (!cancellationSource.IsCancellationRequested)
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        }

        // Ctrl+C lần hai sẽ kết thúc process bình thường.
    };

Console.CancelKeyPress += cancelHandler;

try
{
    return await DotRadarApplication.RunAsync(
        args,
        Console.Out,
        Console.Error,
        cancellationSource.Token);
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}