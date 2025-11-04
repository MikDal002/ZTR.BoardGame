using Spectre.Console.Cli;
using System;
using System.ComponentModel;
// Required for DescriptionAttribute
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ZtrBoardGame.Console.Commands.Base;

namespace ZtrBoardGame.Console.Infrastructure;

public static class CommandOptionExtensions
{
    public static (string[] ProcessedArgs, bool EnableConsoleLogging) ProcessGlobalOptions(this string[] args)
    {
        var processedArgs = args.ToList();

        // Handle --testCommandName
        var testCommandName = GetLongOptionName<GlobalCommandSettings, string?>(s => s.TestCommandName);
        if (!string.IsNullOrEmpty(testCommandName))
        {
            var testCommandNameIndex = processedArgs.FindIndex(a => a.StartsWith(testCommandName, StringComparison.OrdinalIgnoreCase));
            if (testCommandNameIndex != -1)
            {
                var commandArg = processedArgs[testCommandNameIndex];
                processedArgs.RemoveAt(testCommandNameIndex);

                var parts = commandArg.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    var commandValue = parts[1];
                    var commandWords = commandValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    processedArgs.InsertRange(0, commandWords);
                }
            }
        }

        var finalArgs = processedArgs.ToArray();

        // Handle --log-console
        var logConsoleOption = GetLongOptionName<GlobalCommandSettings, bool>(s => s.LogToConsole);
        var enableConsoleLogging = false;
        if (!string.IsNullOrEmpty(logConsoleOption))
        {
            enableConsoleLogging = finalArgs.Contains(logConsoleOption);
        }

        return (finalArgs, enableConsoleLogging);
    }

    /// <summary>
    /// Gets the first long option name (e.g., "--option") from a CommandOptionAttribute
    /// applied to the property selected by the expression.
    /// </summary>
    public static string? GetLongOptionName<TSource, TProperty>(
        Expression<Func<TSource, TProperty>> propertyLambda)
    {
        var propertyInfo = GetPropertyInfo(propertyLambda);

        var commandOptionAttribute = propertyInfo.GetCustomAttribute<CommandOptionAttribute>();
        if (commandOptionAttribute == null)
        {
            return null;
        }

        if (commandOptionAttribute.LongNames.Count > 0)
        {
            return $"--{commandOptionAttribute.LongNames[0]}";
        }

        if (commandOptionAttribute.ShortNames.Count > 0)
        {
            return $"-{commandOptionAttribute.ShortNames[0]}";
        }

        return null;
    }

    /// <summary>
    /// Gets the description from a DescriptionAttribute applied to the property selected by the expression.
    /// </summary>
    public static string? GetDescription<TSource, TProperty>(
        Expression<Func<TSource, TProperty>> propertyLambda)
    {
        var propertyInfo = GetPropertyInfo(propertyLambda);

        var descriptionAttribute = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttribute?.Description;
    }

    private static PropertyInfo GetPropertyInfo<TSource, TProperty>(
        Expression<Func<TSource, TProperty>> propertyLambda)
    {
        if (propertyLambda.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Expression refers to a method, not a property.", nameof(propertyLambda));
        }

        if (memberExpression.Member is not PropertyInfo propertyInfo)
        {
            throw new ArgumentException("Expression refers to a field, not a property.", nameof(propertyLambda));
        }

        return propertyInfo;
    }
}
