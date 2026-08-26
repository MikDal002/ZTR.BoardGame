using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using ZtrBoardGame.Server.Commons;

namespace ZtrBoardGame.Server.Desktop;

public class Program
{
    public static void CreateWindow(IServiceProvider serviceProvider)
    {
        using var app = Application.Create();
        app.Init();

        WrapView wrapPanel = new()
        {
            BorderStyle = LineStyle.Dotted,
            Text = "Połączone plansze",
            X = 0,
            Y = 0,
            HorizontalSpacing = 1,
            Width = Dim.Percent(33),
            Height = Dim.Fill(1),
        };

        var quitShortcut = new Shortcut(Key.Esc, "Wyjście", () => app.RequestStop());
        var connectionStatus = new Shortcut(Key.Empty, "Oczekuje na połączenie...", null);

        var statusBar = new StatusBar(new[] { quitShortcut, connectionStatus });

        var boardStorage = serviceProvider.GetRequiredService<IBoardStorage>();
        var gameViewModel = new GameControlViewModel(boardStorage, serviceProvider.GetRequiredService<IGameService>());
        var gameControlPanel = new GameControlPanel(gameViewModel)
        {
            Title = "Gra",
            X = Pos.Right(wrapPanel), // Automatycznie dokuje się do prawej krawędzi WrapView
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        app.AddTimeout(TimeSpan.FromSeconds(1), () =>
        {
            gameViewModel.RefreshState();

            return true;
        });

        app.AddTimeout(TimeSpan.FromSeconds(1), () =>
        {
            var boardsModels = boardStorage.GetAll();
            var existingViews = wrapPanel.GetSubViews()
                .Cast<BoardControl>()
                .ToList();
            existingViews.ForEach(d => d.ViewModel.Refresh());

            foreach (var board in boardsModels)
            {
                var existingView = existingViews
                    .FirstOrDefault(d => d.ViewModel.BoardModel == board);

                if (existingView is null)
                {
                    wrapPanel.Add(new BoardControl(new BoardViewViewModel(board)));
                }
            }

            var connectedCount = existingViews.Count();

            connectionStatus.Title = connectedCount == 0
                ? "Oczekuje na połączenie..."
                : $"Połączone plansze: {connectedCount}";

            statusBar.SetNeedsDraw();
            return true;
        });

        using Window window = new() { BorderStyle = LineStyle.None };

        window.Add(wrapPanel, gameControlPanel);
        window.Add(statusBar);

        app.Run(window);
    }
}

