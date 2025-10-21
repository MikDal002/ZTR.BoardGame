using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System;

namespace ZtrTemplates.Console;

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public object Resolve(Type? type)
    {
        return _provider.GetRequiredService(type ?? throw new ArgumentNullException(nameof(type)));
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
