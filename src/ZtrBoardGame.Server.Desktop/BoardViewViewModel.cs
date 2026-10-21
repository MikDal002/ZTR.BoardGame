using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using ZtrBoardGame.Server.Commons;

namespace ZtrBoardGame.Server.Desktop;

public partial class BoardViewViewModel(Board boardModel) : ObservableObject
{
    public Board BoardModel => boardModel;
    // --- Podstawowe informacje ---
    [ObservableProperty] public partial string DeviceName { get; set; } = "Nieznane urządzenie";
    [ObservableProperty] public partial string IpAddress { get; set; } = "___.___.___.___";
    [ObservableProperty] public partial bool IsConnected { get; set; }
    [ObservableProperty] public partial string StatusMessage { get; set; } = "";

    // --- Dane diagnostyczne (Sieć / HealthCheck) ---
    [ObservableProperty] public partial string LastHealthCheckTime { get; set; } = "Brak";
    [ObservableProperty] public partial string HealthCheckPing { get; set; } = "--- ms";

    // --- Dane diagnostyczne (Stan Gry) ---
    [ObservableProperty] public partial string GameRequestedTime { get; set; } = "Brak";
    [ObservableProperty] public partial string GameResultTime { get; set; } = "Brak";
    [ObservableProperty] public partial string GameDuration { get; set; } = "---";

    public void Refresh()
    {
        // 1. Podstawowe dane z URI
        DeviceName = boardModel.Address.Host;
        IpAddress = boardModel.Address.ToString();

        // 2. Diagnostyka HealthCheck
        boardModel.GetHealthStatus(out var lastHealthCheck, out var duration);
        IsConnected = lastHealthCheck.HasValue;

        // Formatujemy czasy (np. tylko godzina:minuta:sekunda dla czytelności)
        LastHealthCheckTime = lastHealthCheck?.ToLocalTime().ToString("HH:mm:ss") ?? "Brak";
        HealthCheckPing = duration.HasValue ? $"{duration.Value.TotalMilliseconds:F0} ms" : "--- ms";

        // 3. Status opisowy
        StatusMessage = boardModel.DescriptionStatus ?? "Brak dodatkowych informacji";

        // 4. Diagnostyka logiki gry
        GameRequestedTime = boardModel.RequestedGameStartOn?.ToLocalTime().ToString("HH:mm:ss") ?? "Brak";

        if (boardModel.GameResult != null)
        {
            GameResultTime = boardModel.ResultArrivedOn?.ToLocalTime().ToString("HH:mm:ss") ?? "Brak";
            GameDuration = $"{boardModel.GameResult.Duration.TotalSeconds:F2} s";
        }
        else
        {
            GameResultTime = "Brak";
            GameDuration = "---";
        }
    }
}

public sealed class BoardControl : FrameView
{
    public BoardViewViewModel ViewModel { get; private init; }

    // Elementy interfejsu
    private readonly Label _lblIpAddress;
    private readonly Label _lblStatus;
    private readonly Label _lblHealth;
    private readonly Label _lblGameStart;
    private readonly Label _lblGameEnd;
    private readonly Label _lblMessage;

    public BoardControl(BoardViewViewModel viewModel)
    {
        ViewModel = viewModel;

        // Zwiększamy wysokość i lekko szerokość, aby pomieścić nowe dane diagnostyczne
        Width = 42;
        Height = 9;

        // 1. Inicjalizacja kontrolek
        _lblIpAddress = new Label { X = 1, Y = 0, Width = Dim.Fill() };
        _lblStatus = new Label { X = 1, Y = 1, Width = Dim.Fill() };

        // Diagnostyka sieci
        _lblHealth = new Label { X = 1, Y = 2, Width = Dim.Fill() };

        // Diagnostyka gry
        _lblGameStart = new Label { X = 1, Y = 3, Width = Dim.Fill() };
        _lblGameEnd = new Label { X = 1, Y = 4, Width = Dim.Fill() };

        // Wiadomość (zostawiamy pustą linię przerwy wyżej)
        _lblMessage = new Label { X = 1, Y = 6, Width = Dim.Fill(1) };

        Add(_lblIpAddress, _lblStatus, _lblHealth, _lblGameStart, _lblGameEnd, _lblMessage);

        // 2. Pierwsze wczytanie danych z ViewModelu
        UpdateUi();

        // 3. Nasłuchiwanie na zmiany
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Wywołanie na głównym wątku Terminal.Gui
        App.Invoke(UpdateUi);
    }

    private void UpdateUi()
    {
        // Tytuł
        Title = ViewModel.DeviceName;

        // Podstawowe dane
        _lblIpAddress.Text = $"IP: {ViewModel.IpAddress}";
        _lblMessage.Text = $"Msg: {ViewModel.StatusMessage}";

        // Status połączenia
        if (ViewModel.IsConnected)
        {
            _lblStatus.Text = "Status: [Połączono]";
        }
        else
        {
            _lblStatus.Text = "Status: [Brak połączenia]";
        }

        // Diagnostyka HealthCheck
        _lblHealth.Text = $"HC: {ViewModel.LastHealthCheckTime} (Ping: {ViewModel.HealthCheckPing})";

        // Diagnostyka Gry
        _lblGameStart.Text = $"Req Start: {ViewModel.GameRequestedTime}";
        _lblGameEnd.Text = $"Req End: {ViewModel.GameResultTime} (Czas: {ViewModel.GameDuration})";

        SetNeedsDraw();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        base.Dispose(disposing);
    }
}
