using OBSWebsocketDotNet;
using Scalar.AspNetCore;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Collections.Generic;

// --- Force the app to run in its own directory, not System32 ---
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5018); // HTTP port
});

// --- 1. Add Services ---
builder.Host.UseWindowsService();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<OBSWebsocket>();

var app = builder.Build();

// --- LOGGING & FLUSHING SYSTEM ---
var logFilePath = Path.Combine(AppContext.BaseDirectory, "obs_api_logs.txt");
object fileLock = new object();

void LogMessage(string message)
{
    try
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
        lock (fileLock)
        {
            File.AppendAllText(logFilePath, logEntry);
        }
    }
    catch { }
}

void FlushOldLogs()
{
    try
    {
        lock (fileLock)
        {
            if (!File.Exists(logFilePath)) return;

            int daysToKeep = 7;
            DateTime cutoff = DateTime.Now.AddDays(-daysToKeep);

            var allLines = File.ReadAllLines(logFilePath);
            var keptLines = new List<string>();

            foreach (var line in allLines)
            {
                if (line.Length > 21 && line.StartsWith("["))
                {
                    string dateStr = line.Substring(1, 19);
                    if (DateTime.TryParse(dateStr, out DateTime logDate))
                    {
                        if (logDate >= cutoff) keptLines.Add(line);
                        continue;
                    }
                }
                keptLines.Add(line);
            }
            File.WriteAllLines(logFilePath, keptLines);
        }
    }
    catch { }
}

// Log Flushing Background Loop
Task.Run(async () =>
{
    while (true)
    {
        FlushOldLogs();
        await Task.Delay(TimeSpan.FromHours(24));
    }
});

LogMessage("=== OBS API Service Starting ===");


// --- 2. Enable OpenAPI & Scalar UI ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


// --- 3. THE WATCHDOG (OBS Connection Monitor) ---
var obs = app.Services.GetRequiredService<OBSWebsocket>();

// Events are now ONLY used for writing to the log file. They do NOT trigger logic.
obs.Connected += (sender, args) => LogMessage("Successfully connected to OBS!");
obs.Disconnected += (sender, args) => LogMessage("OBS Connection Dropped.");

// The permanent Watchdog loop
Task.Run(async () =>
{
    while (true)
    {
        try
        {
            // If disconnected, step in and fix it
            if (!obs.IsConnected)
            {
                LogMessage("Attempting to connect to OBS...");

                // Clear the corrupted socket
                obs.Disconnect();

                // CRITICAL: Give the OS 1 second to fully release the network port to prevent freezing
                await Task.Delay(1000);

                // Use ConnectAsync so the thread doesn't lock up if OBS is totally unresponsive
                obs.ConnectAsync("ws://127.0.0.1:4455", "nhtQl1pidCC5qHCe");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Watchdog Error: {ex.Message}");
        }

        // Wait 5 seconds before checking the connection status again
        await Task.Delay(5000);
    }
});


// --- 4. Endpoints ---
app.MapGet("/", () => Results.Content("<h1>OBS API is Running</h1><p>Check the <a href='/scalar/v1'>API Reference (Scalar)</a></p>", "text/html"));
app.MapGet("/status", () => new { connected = obs.IsConnected });

app.MapGet("/scenes", () =>
{
    if (!obs.IsConnected) return Results.Problem("OBS not connected");

    var sceneList = obs.GetSceneList();
    return Results.Ok(sceneList.Scenes.Select(s => s.Name));
});

app.MapGet("/scene/current", () =>
{
    if (!obs.IsConnected) return Results.Problem("OBS not connected");
    return Results.Ok(new { current = obs.GetCurrentProgramScene() });
});

app.MapPost("/scene/{name}", (string name) =>
{
    if (!obs.IsConnected) return Results.Problem("OBS not connected");

    try
    {
        LogMessage($"API Request: Switching scene to {name}");
        obs.SetCurrentProgramScene(name);
        return Results.Ok($"Switched to {name}");
    }
    catch (Exception ex)
    {
        LogMessage($"API Request Failed (Switch Scene): {ex.Message}");
        return Results.BadRequest(ex.Message);
    }
});

app.Run();