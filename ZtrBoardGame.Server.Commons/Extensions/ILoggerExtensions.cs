using Microsoft.Extensions.Logging;

namespace ZtrBoardGame.Server.Commons.Extensions;

public static class ILoggerExtensions
{
    public static IDisposable? BeginScopeWith(this ILogger logger, params IList<(string Key, string Value)> values)
        => logger.BeginScope(values.ToDictionary(d => d.Key, d => d.Value));
}
