using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

public class CustomWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    private string _command;
    private Action<IServiceCollection> _serviceConfiguration;
    public string HostUrl { get; set; }
    public CustomWebApplicationFactory(string command)
    {
        _command = command;
    }

    public void ConfigureTestServices(Action<IServiceCollection> serviceConfiguration)
    {
        _serviceConfiguration = serviceConfiguration;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        if (!string.IsNullOrWhiteSpace(HostUrl))
        {
            builder.UseUrls(HostUrl);
        }

        builder.UseSetting("testCommandName", _command);
        builder.ConfigureServices(services =>
        {
            _serviceConfiguration?.Invoke(services);
        });
    }
}
