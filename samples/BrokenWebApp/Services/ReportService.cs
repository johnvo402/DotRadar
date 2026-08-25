namespace BrokenWebApp.Services;

public sealed class ReportService
{
    public async Task GenerateAsync(
        CancellationToken cancellationToken)
    {
        await Task.Delay(100);
    }
}