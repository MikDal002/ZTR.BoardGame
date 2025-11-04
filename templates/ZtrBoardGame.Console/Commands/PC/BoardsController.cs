using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.Commands.PC;

[ApiController]
[Route("api/boards")]
public class BoardsController : ControllerBase
{
    private readonly ILogger<BoardsController> _logger;
    private readonly IBoardStorage _boardStorage;
    private readonly IAnsiConsole _console;

    public BoardsController(ILogger<BoardsController> logger, IBoardStorage boardStorage, IAnsiConsole console)
    {
        _logger = logger;
        _boardStorage = boardStorage;
        _console = console;
    }

    public record GetBoardsOutDto(int Count);
    [HttpGet]
    public ActionResult<GetBoardsOutDto> Get()
    {
        return new GetBoardsOutDto(_boardStorage.Count);
    }

    [HttpPost]

    // 
    // This known security issue is now tracked in GitHub issue #9.
    // See: https://github.com/MikDal002/ZTR.BoardGame/issues/9
    //
    // This endpoint contains security issue that the server will be invoking not checked address.
    // It should be handled in production version of application. As it is a PoC we can leave it as is.
    //
    public IActionResult Post([FromQuery] string responseAddress)
    {
        var boardIpAddress = new Uri(responseAddress);

        var _ = _logger.BeginScopeWith(("BoardIpAddress", boardIpAddress.ToString()));

        if (boardIpAddress is null)
        {
            _logger.LogWarning("Request received without a remote IP address.");
            return BadRequest("Could not determine remote IP address.");
        }

        _logger.LogInformation("Received hello from board");
        _console.MarkupLine("Received hello from board");

        _boardStorage.Add(boardIpAddress);
        return Ok();
    }
}
