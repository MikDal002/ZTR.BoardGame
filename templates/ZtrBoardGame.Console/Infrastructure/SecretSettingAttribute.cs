using System;

namespace ZtrTemplates.Console.Infrastructure;

/// <summary>
/// Indicates that a command setting property should be treated as secret
/// and its value should be masked or omitted from logs.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SecretSettingAttribute : Attribute
{
}
