using Spectre.Console.Cli;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.Commands.Base;

/// <summary>
/// Base class for Spectre.Console commands that support cancellation via Ctrl+C or process exit.
/// </summary>
/// <typeparam name="TSettings">The command settings type.</typeparam>
public abstract class CancellableAsyncCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    private readonly Lazy<ConsoleAppCancellationTokenSource> _cancellationTokenSource =
        new(() => new());

    /// <summary>
    /// Executes the command asynchronously with cancellation support.
    /// This method is sealed and cannot be overridden by derived classes.
    /// Implement the cancellation-aware logic in <see cref="ExecuteAsync(CommandContext, TSettings, CancellationToken)"/>.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>The exit code.</returns>
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        try
        {
            var cancellationToken = _cancellationTokenSource.Value.Token;
            return await ExecuteAsync(context, settings, cancellationToken);
        }
        finally
        {
            if (_cancellationTokenSource.IsValueCreated)
            {
                _cancellationTokenSource.Value.Dispose();
            }
        }
    }

    /// <summary>
    /// Executes the command asynchronously with cancellation support.
    /// Derived classes must implement this method to provide the command logic.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, returning the exit code.</returns>
    public abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken);
}
