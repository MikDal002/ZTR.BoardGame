using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ZtrBoardGame.Console.Commands.Board;

[ApiController]
[Route("api/board/health")]
public class HealthController(IBoardStatusStorage boardStatusStorage, IAnsiConsole console, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { WasHelloReceived = boardStatusStorage.WasHelloReceived });
    }

    [HttpPost]
    public IActionResult Post()
    {
        var message = "Received hello from PC";
        logger.LogInformation(message);
        console.WriteLine(message);
        boardStatusStorage.WasHelloReceived = true;
        return Ok();
    }
}
