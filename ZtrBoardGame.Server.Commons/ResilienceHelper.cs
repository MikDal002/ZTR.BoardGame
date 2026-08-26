using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ZtrBoardGame.Server.Commons;

public record ResilienceSettings(int MaxRetries, TimeSpan Delay, string OperationName, string TargetName);

public static class ResilienceHelper
{
    public static async Task<bool> InvokeWithRetryAsync(Func<Task> func, ResilienceSettings settings, IAnsiConsole console, ILogger logger, CancellationToken cancellationToken, Uri? targetAddress, Action<Exception>? onError = null)
    {
        var trials = settings.MaxRetries;

        while (!cancellationToken.IsCancellationRequested)
        {
            Exception? prevException = null;
            try
            {
                await func();
                return true;
            }
            catch (HttpRequestException e)
            {
                console.MarkupLine($"[red]Cannot connect to {settings.TargetName} {targetAddress}. Reason: {e.Message}[/]");
                logger.LogError(e, "Failed to {Operation} to {TargetName}", settings.OperationName, settings.TargetName);
                prevException = e;
            }
            catch (TaskCanceledException e) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(e, "{Operation} to {TargetName} timed out (TaskCanceledException), retrying...", settings.OperationName, settings.TargetName);
                prevException = e;
            }
            catch (TaskCanceledException e)
            {
                logger.LogInformation(e, "{Operation} task was canceled by user or system.", settings.OperationName);
                return false;
            }
            catch (Exception e)
            {
                logger.LogError(e, "An unexpected error occurred during {Operation} to {TargetName}", settings.OperationName, settings.TargetName);
                prevException = e;
            }

            onError?.Invoke(prevException);

            trials--;
            if (trials <= 0)
            {
                logger.LogError(prevException, "Failed to {Operation} to {TargetName} over {MaxRetries} times. Giving up.", settings.OperationName, settings.TargetName, settings.MaxRetries);
                throw new TimeoutException($"Failed to {settings.OperationName} to {settings.TargetName} over {settings.MaxRetries} times.", prevException);
            }

            logger.LogDebug("Waiting {Delay} before next attempt to {Operation}", settings.Delay, settings.OperationName);
            await Task.Delay(settings.Delay, cancellationToken);
        }

        logger.LogInformation("{Operation} was canceled (cancellationToken).", settings.OperationName);
        return false;
    }
}
