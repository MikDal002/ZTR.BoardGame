using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZtrBoardGame.Console.Infrastructure;

public record ResilienceSettings(int MaxRetries, TimeSpan Delay, string OperationName, string TargetName);

public static class ResilienceHelper
{
    public static async Task InvokeWithRetryAsync(Func<Task> func, ResilienceSettings settings, IAnsiConsole console, ILogger logger, CancellationToken cancellationToken, Uri? targetAddress)
    {
        var trials = settings.MaxRetries;

        while (!cancellationToken.IsCancellationRequested)
        {
            Exception? prevException = null;
            try
            {
                await func();
                break;
            }
            catch (HttpRequestException e)
            {
                console.MarkupLine($"[red]Cannot connect to {settings.TargetName} {targetAddress}. Reason: {e.Message}[/]");
                logger.LogError(e, "Failed to {Operation} to {TargetName}", settings.OperationName, settings.TargetName);
                prevException = e;
            }
            catch (TaskCanceledException e)
            {
                logger.LogInformation(e, "{Operation} task was canceled.", settings.OperationName);
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "An unexpected error occurred during {Operation} to {TargetName}", settings.OperationName, settings.TargetName);
                prevException = e;
            }

            trials--;
            if (trials <= 0)
            {
                throw new TimeoutException($"Failed to {settings.OperationName} to {settings.TargetName} over {settings.MaxRetries} times.", prevException);
            }

            await Task.Delay(settings.Delay, cancellationToken);
        }
    }
}
