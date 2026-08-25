var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/healthy", async () =>
{
    await Task.Delay(10);
    return Results.Ok(42);
});

app.MapGet("/broken", () =>
{
    Task<int> operation = Task.FromResult(42);

    var result = operation.Result;

    return Results.Ok(result);
});

app.Run();