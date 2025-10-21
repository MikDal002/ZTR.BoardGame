using System;
using System.Threading;

namespace ZtrBoardGame.Console.Infrastructure;

/// <summary>
/// Manages a CancellationTokenSource that gets cancelled on console cancel key press (Ctrl+C) or process exit.
/// </summary>
public sealed class ConsoleAppCancellationTokenSource : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleAppCancellationTokenSource"/> class.
    /// </summary>
    public ConsoleAppCancellationTokenSource()
    {
        _cts = new();
        System.Console.CancelKeyPress += OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    /// <summary>
    /// Gets the CancellationToken associated with this source.
    /// </summary>
    public CancellationToken Token => _cts.Token;

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        // Prevent the process from terminating immediately
        e.Cancel = true;
        RequestCancellation();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        RequestCancellation();
    }

    private void RequestCancellation()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }

        UnsubscribeEvents();
    }

    private void UnsubscribeEvents()
    {
        System.Console.CancelKeyPress -= OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
    }

    /// <summary>
    /// Releases the resources used by the <see cref="ConsoleAppCancellationTokenSource"/>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnsubscribeEvents();
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel(); // Ensure cancellation is requested on dispose
        }

        _cts.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
