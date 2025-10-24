using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZtrBoardGame.Console.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloController : ControllerBase
{
    private readonly ILogger<HelloController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HelloController(ILogger<HelloController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (remoteIpAddress is null)
        {
            _logger.LogWarning("Request received without a remote IP address.");
            return BadRequest("Could not determine remote IP address.");
        }

        _logger.LogInformation("Received hello from board at {RemoteIpAddress}", remoteIpAddress);

        try
        {
            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync($"http://{remoteIpAddress}/api/hello", null, HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Responded with hello to board at {RemoteIpAddress}", remoteIpAddress);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Failed to respond to board at {RemoteIpAddress}. Reason: {Reason}", remoteIpAddress, e.Message);
        }

        return Ok();
    }
}
