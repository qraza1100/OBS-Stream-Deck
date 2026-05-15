using OBSWebsocketDotNet;
using Scalar.AspNetCore; // Requires: dotnet add package Scalar.AspNetCore
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5018); // HTTP port
});
// --- 1. Add Services ---
builder.Host.UseWindowsService(); // Enables background service behavior
builder.Services.AddOpenApi();
builder.Services.AddSingleton<OBSWebsocket>();

var app = builder.Build();

// --- 2. Enable OpenAPI & Scalar UI ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Generates /openapi/v1.json
    app.MapScalarApiReference(); // Visual UI at /scalar/v1
}

// --- 3. OBS Connection Logic ---
var obs = app.Services.GetRequiredService<OBSWebsocket>();

// Background task to handle the handshake
Task.Run(() =>
{
    try
    {
        // Using your verified credentials from the screenshots
        obs.ConnectAsync("ws://127.0.0.1:4455", "nhtQl1pidCC5qHCe");
        Console.WriteLine("Successfully connected to OBS!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"OBS Connection Error: {ex.Message}");
    }
});

// --- 4. Endpoints ---

// Home page redirecting to the UI
app.MapGet("/", () => Results.Content("<h1>OBS API is Running</h1><p>Check the <a href='/scalar/v1'>API Reference (Scalar)</a></p>", "text/html"));

// Status endpoint (Use this to check if you're actually connected)
app.MapGet("/status", () => new { connected = obs.IsConnected });

// GET ALL SCENES
app.MapGet("/scenes", () =>
{
    if (!obs.IsConnected) return Results.Problem("OBS not connected");

    var sceneList = obs.GetSceneList();
    return Results.Ok(sceneList.Scenes.Select(s => s.Name));
});

// GET CURRENT SCENE
app.MapGet("/scene/current", () =>
{
    if (!obs.IsConnected) return Results.Problem("OBS not connected");
    return Results.Ok(new { current = obs.GetCurrentProgramScene() });
});

// SWITCH SCENE
app.MapPost("/scene/{name}", (string name) =>
{
    if (!obs.IsConnected) return Results.Problem("OBS not connected");

    try
    {
        if (name == "Gameplay")
        {
            //var cs2Settings = new Newtonsoft.Json.Linq.JObject
            //{
            //    ["window"] = "[cs2.exe]: Counter-Strike 2"
            //};

            //// Update the settings for the input named "CS"
            //obs.SetInputSettings("CS", cs2Settings);
        }
        obs.SetCurrentProgramScene(name);
        return Results.Ok($"Switched to {name}");
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();