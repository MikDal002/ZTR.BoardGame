using System.ComponentModel;
using System.Data;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace ZtrBoardGame.Server.Desktop;

public sealed class GameControlPanel : FrameView
{
    private readonly GameControlViewModel _viewModel;

    // --- Kontenery na poszczególne stany ---
    private readonly View _startView;
    private readonly View _inProgressView;
    private readonly View _leaderboardView;

    // --- Dynamiczne kontrolki ---
    private readonly Label _lblConnectedCount;
    private readonly Button _btnStart;
    private readonly Label _awaitingBoardsLabel;
    readonly Label _lblWithLeaderBoard;
    readonly TableView _leaderboardTable;

    public GameControlPanel(GameControlViewModel viewModel)
    {
        _viewModel = viewModel;
        Title = " Centrum Dowodzenia ";

        // 1. Budowa ekranu Startowego
        _startView = new View { Width = Dim.Fill(), Height = Dim.Fill() };

        _lblConnectedCount = new Label
        {
            X = Pos.Center(),
            Y = Pos.Center() - 2
        };

        _btnStart = new Button
        {
            Text = "Rozpocznij Grę",
            X = Pos.Center(),
            Y = Pos.Center()
        };
        // Wykonanie komendy ICommand z naszego ViewModelu
        _btnStart.Accepted += (_, _) => _viewModel.StartGameCommand.Execute(null);

        _startView.Add(_lblConnectedCount, _btnStart);

        // 2. Budowa ekranu Gry w toku
        _inProgressView = new View { Width = Dim.Fill(), Height = Dim.Fill() };

        var progressFrame = new FrameView()
        {
            Text = "Status Rozgrywki",
            X = Pos.Center(),
            Y = Pos.Center() - 2,
            Width = 60, // szerokość naszej ramki
            Height = 5  // wysokość naszej ramki
        };
        _awaitingBoardsLabel = new Label { Text = "Oczekiwanie na zakończenie rozgrywki", X = Pos.Center(), Y = Pos.Center() };

        progressFrame.Add(_awaitingBoardsLabel);
        _inProgressView.Add(progressFrame);

        // 3. Budowa ekranu Wyników (Leaderboard)
        _leaderboardView = new View { Width = Dim.Fill(), Height = Dim.Fill() };

        var lblLeaderboardTitle = new Label { Text = "--- TABELA WYNIKÓW ---", X = Pos.Center(), Y = 1 };

        _leaderboardTable = new TableView()
        {
            X = Pos.Center(),
            Y = 3,
            Width = 60, // Ograniczamy szerokość, żeby ładnie wyglądało
            Height = Dim.Fill(4), // Wypełnia w dół, ale zostawia miejsce na przycisk
            FullRowSelect = true
        };
        var lblHint = new Label { Text = "Wciśnij poniższy przycisk, aby powrócić do Lobby...", X = Pos.Center(), Y = Pos.AnchorEnd(3) };

        var btnReset = new Button { Text = "Nowa Gra", X = Pos.Center(), Y = Pos.AnchorEnd(2) };
        btnReset.Accepted += (_, _) => _viewModel.ResetGameCommand.Execute(null);

        _leaderboardView.Add(lblLeaderboardTitle, _leaderboardTable, lblHint, btnReset);

        // 4. Składamy wszystko w jedną ramkę
        Add(_startView, _inProgressView, _leaderboardView);

        // 5. Inicjalizacja i podpięcie pod MVVM
        UpdateVisibility();
        UpdateDynamicData();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Gwarancja wykonania na głównym wątku UI
        App.Invoke(() =>
        {
            if (e.PropertyName == nameof(GameControlViewModel.CurrentState))
            {
                UpdateVisibility();
                UpdateDynamicData();
            }
            else if (e.PropertyName == nameof(GameControlViewModel.ConnectedBoards))
            {
                UpdateDynamicData();
            }
            else if (e.PropertyName == nameof(GameControlViewModel.BoardsWithResults))
            {
                UpdateDynamicData();
            }
        });
    }

    private void UpdateVisibility()
    {
        _startView.Visible = _viewModel.CurrentState == GameState.WaitingForPlayers;
        _inProgressView.Visible = _viewModel.CurrentState == GameState.GameInProgress;
        _leaderboardView.Visible = _viewModel.CurrentState == GameState.Leaderboard;

        if (_viewModel.CurrentState == GameState.Leaderboard)
        {
            // Używamy DataTable z System.Data do zasilenia kontrolki TableView
            var dt = new DataTable();
            dt.Columns.Add("Miejsce");
            dt.Columns.Add("Board");
            dt.Columns.Add("Czas (ms)");

            var place = 1;
            foreach (var b in _viewModel.Leaders)
            {
                dt.Rows.Add(place.ToString(),
                    b.Address.ToString(),
                    b.GameResult!.Duration.TotalMilliseconds.ToString());
                place++;
            }

            // Podpinamy wygenerowane dane pod tabelę
            _leaderboardTable.Table = new DataTableSource(dt);
        }

        SetNeedsDraw();
    }

    private void UpdateDynamicData()
    {
        if (_startView.Visible)
        {
            var btnStartEnabled = _viewModel.StartGameCommand.CanExecute(null);

            _lblConnectedCount.Text = btnStartEnabled
                ? "Gotowy do gry!"
                : "Brak połączonych plansz (oczekiwanie...)";

            _btnStart.Enabled = btnStartEnabled;
        }
        else if (_inProgressView.Visible)
        {
            var withResult = _viewModel.BoardsWithResults;

            var statusPanel =
                $"Gra w toku... Pozostało do zebrania: {withResult} / {_viewModel.ConnectedBoards}";
            _inProgressView.Text = statusPanel;

        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        base.Dispose(disposing);
    }
}
