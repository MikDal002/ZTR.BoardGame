using Spectre.Console.Cli;
using System;
using System.ComponentModel; // Required for DescriptionAttribute
using System.Linq.Expressions;
using System.Reflection;

namespace ZtrTemplates.Console.Infrastructure;

public static class CommandOptionExtensions
{
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
