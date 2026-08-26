using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZtrBoardGame.Console.Commands.PC.UI;

public interface IAvaloniaGameDashboard
{
    Task StartUI(CancellationToken cancellationToken);
}

class AvaloniaGameDashboard(IServiceProvider serviceProvider) : IAvaloniaGameDashboard
{
    public Task StartUI(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();

        var uiThread = new Thread(() =>
        {
            try
            {
                // W Terminal.Gui v2 nie musimy już używać BuildAvaloniaApp
                ZtrBoardGame.Server.Desktop.Program.CreateWindow(serviceProvider);

                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                throw;
            }
        });

        // Wątek STA nie jest już wymagany dla Terminal.Gui, ale zostawiamy osobny wątek
        // by nie blokować głównego zadania asynchronicznego.
        uiThread.Start();

        cancellationToken.Register(() => tcs.TrySetCanceled());
        return tcs.Task;
    }
}
