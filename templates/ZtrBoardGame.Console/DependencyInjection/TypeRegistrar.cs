using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System;
using System.IO;
using ZtrBoardGame.Configuration.Shared;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.DependencyInjection;

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(bool enableConsoleLogging)
    {
        _services = new ServiceCollection();

        // --- Configuration Setup ---
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            // IF file doesn't exists run _build project first.
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);

        LoggerSetup.ConfigureSerilog(_services, configuration, enableConsoleLogging); // Call the new static method
        _services.AddSingleton<ICommandInterceptor, LogInterceptor>();

        _services.Configure<UpdateOptions>(configuration.GetSection(nameof(UpdateOptions)));
        _services.AddSingleton<IUpdateService, UpdateService>();
    }

    public ITypeResolver Build() => new TypeResolver(_services.BuildServiceProvider());

    public void Register(Type service, Type implementation) => _services.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) => _services.AddSingleton(service, implementation);
    public void RegisterLazy(Type service, Func<object> factory) => _services.AddSingleton(service, _ => factory());
}
