using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

public class CustomWebApplicationFactory<TEntryPoint>(string command) : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    private Action<IServiceCollection> _serviceConfiguration;
    public string HostUrl { get; set; }

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

        builder.UseSetting("testCommandName", command);
        builder.ConfigureServices(services =>
        {
            _serviceConfiguration?.Invoke(services);
        });
    }
}
