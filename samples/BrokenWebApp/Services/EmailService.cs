namespace BrokenWebApp.Services;

public sealed class EmailService
{
    public async void SendAsync()
    {
        await Task.Delay(10);
    }
}