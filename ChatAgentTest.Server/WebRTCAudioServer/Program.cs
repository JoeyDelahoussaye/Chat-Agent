using System.Net;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = new string[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<AudioStreamClient>(); 
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Loopback, 5001, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

var app = builder.Build();
app.UseCors(builder =>
{
    builder.WithOrigins("http://localhost:4200")
           .AllowAnyHeader()
           .AllowAnyMethod()
           .AllowCredentials();
});

app.UseWebSockets();
app.Map("/ws", async (HttpContext context, AudioStreamClient openAiClient) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        try
        {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine("WebSocket connected");

            await openAiClient.Connect();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("WebSocket connection failed.");
            }
        }
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Invalid WebSocket request.");
    }
});

app.Run();
