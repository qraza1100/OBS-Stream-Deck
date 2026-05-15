using System.Text.Json;

namespace StreamDeckApp;

public partial class MainPage : ContentPage
{
    private readonly string baseUrl = "http://192.168.1.2:5018";
    private static readonly HttpClient client = new HttpClient();
    private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    // To prevent the picker from triggering a switch while it's being populated
    private bool _isUpdatingList = false;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFullStatus();
    }

    private async Task LoadFullStatus()
    {
        await CheckConnectionStatus();
        await LoadScenesIntoPicker();
        await LoadCurrentScene();
    }

    private async Task CheckConnectionStatus()
    {
        try
        {
            var json = await client.GetStringAsync($"{baseUrl}/status");
            var doc = JsonDocument.Parse(json);
            bool isConnected = doc.RootElement.GetProperty("connected").GetBoolean();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = isConnected ? "OBS Connected" : "OBS Disconnected";
                StatusDot.BackgroundColor = isConnected ? Color.FromArgb("#00FF00") : Color.FromArgb("#FF0000");
            });
        }
        catch
        {
            StatusLabel.Text = "API Unreachable";
            StatusDot.BackgroundColor = Color.FromArgb("#FF0000");
        }
    }

    async Task LoadScenesIntoPicker()
    {
        try
        {
            _isUpdatingList = true;
            var json = await client.GetStringAsync($"{baseUrl}/scenes");
            var sceneNames = JsonSerializer.Deserialize<List<string>>(json, jsonOptions);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ScenePicker.ItemsSource = sceneNames;
            });
        }
        catch (Exception ex) { Console.WriteLine($"Picker Load Error: {ex.Message}"); }
        finally { _isUpdatingList = false; }
    }

    async Task LoadCurrentScene()
    {
        try
        {
            var json = await client.GetStringAsync($"{baseUrl}/scene/current");
            var doc = JsonDocument.Parse(json);
            var current = doc.RootElement.GetProperty("current").GetString();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentSceneLabel.Text = $"Current: {current}";
            });
        }
        catch (Exception ex) { Console.WriteLine($"Current Scene Load Error: {ex.Message}"); }
    }

    private async void OnScenePickerChanged(object sender, EventArgs e)
    {
        // Don't trigger if the list is just being refreshed or nothing is selected
        if (_isUpdatingList || ScenePicker.SelectedIndex == -1) return;

        var selectedScene = ScenePicker.SelectedItem.ToString();

        try
        {
            var response = await client.PostAsync($"{baseUrl}/scene/{Uri.EscapeDataString(selectedScene)}", null);
            if (response.IsSuccessStatusCode)
            {
                // Wait briefly for OBS to process the transition
                await Task.Delay(500);
                await LoadCurrentScene();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Switch Error", ex.Message, "OK");
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e) => await LoadFullStatus();
}