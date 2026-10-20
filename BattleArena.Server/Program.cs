
using BattleArena.Server.Networking;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConnectionGroupManager>();
builder.Services.AddSingleton<WebSocketHandler>();

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/", () => "BattleArena server is running.");

app.Map("/ws", async context =>
{
    var handler = context.RequestServices
        .GetRequiredService<WebSocketHandler>();

    await handler.HandleAsync(context);
});

app.Run();
