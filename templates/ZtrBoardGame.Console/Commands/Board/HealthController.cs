using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ZtrBoardGame.Console.Commands.Board;

[ApiController]
[Route("api/board/health")]
public class HealthController(IAnsiConsole console, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var message = "Received hello from PC";
        logger.LogInformation(message);
        console.WriteLine(message);
        return Ok();
    }
}
