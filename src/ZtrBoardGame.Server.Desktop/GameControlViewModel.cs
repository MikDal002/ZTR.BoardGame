using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Server.Commons;

namespace ZtrBoardGame.Server.Desktop;

public enum GameState
{
    INVALID,
    WaitingForPlayers,
    GameInProgress,
    Leaderboard
}

public partial class GameControlViewModel(IBoardStorage boardStorage, IGameService gameService) : ObservableObject
{
    [ObservableProperty] public partial GameState CurrentState { get; set; } = GameState.WaitingForPlayers;
    [ObservableProperty] public partial IReadOnlyCollection<Board> Leaders { get; private set; } = new List<Board>();
    #region ShowLeaderBoard

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowLeaderboardCommand))]
    public partial int BoardsWithResults { get; set; } = 0;

    private bool CanShowLeaderboard() => BoardsWithResults == ConnectedBoards && ConnectedBoards > 0;

    [RelayCommand(CanExecute = nameof(CanShowLeaderboard))]
    private void ShowLeaderboard()
    {
        var finalBoards = boardStorage.GetAll().ToList();
        var sortedResults = finalBoards.Where(b => b.GameResult != null)
            .OrderBy(b => b.GameResult!.Duration).ToList();
        Leaders = sortedResults;
        CurrentState = GameState.Leaderboard;
    }

    #endregion

    #region StartGame
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartGameCommand))]
    public partial int ConnectedBoards { get; set; }
    private bool CanStartGame() => ConnectedBoards > 0;

    [RelayCommand(CanExecute = nameof(CanStartGame))]
    private async Task StartGame()
    {
        try
        {
            CurrentState = GameState.GameInProgress;
            await gameService.StartSessionAsync(CancellationToken.None);
        }
        catch
        {
            CurrentState = GameState.WaitingForPlayers;
        }
    }
    #endregion

    public void RefreshState()
    {
        ConnectedBoards = boardStorage.GetAll().Count();
        BoardsWithResults = boardStorage.GetAll().Count(d => d.GameResult is not null);

        if (ShowLeaderboardCommand.CanExecute(null))
        {
            ShowLeaderboardCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void ResetGame()
    {
        // 1. Resetujemy stan wszystkich plansz
        foreach (var board in boardStorage.GetAll())
        {
            board.Reset();
        }

        // 2. Wracamy do ekranu startowego
        CurrentState = GameState.WaitingForPlayers;
    }
}
