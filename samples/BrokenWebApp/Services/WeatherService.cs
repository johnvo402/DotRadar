namespace BrokenWebApp.Services;

public sealed class WeatherService
{
    public async Task<string> GetAsync()
    {
        using var client = new HttpClient();

        return await client.GetStringAsync(
            "https://example.com");
    }
}