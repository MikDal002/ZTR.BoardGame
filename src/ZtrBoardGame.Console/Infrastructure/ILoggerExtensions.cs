using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZtrBoardGame.Console.Infrastructure;

public static class ILoggerExtensions
{
    public static IDisposable? BeginScopeWith(this ILogger logger, params IList<(string Key, string Value)> values)
        => logger.BeginScope(values.ToDictionary(d => d.Key, d => d.Value));
}
