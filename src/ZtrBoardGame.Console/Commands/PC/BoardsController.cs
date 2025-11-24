using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.Commands.PC;

[ApiController]
[Route("api/boards")]
public class BoardsController(IBoardStorage boardStorage, IGameService gameService, IAnsiConsole console, ILogger<BoardsController> logger)
    : ControllerBase
{
    public record GetBoardsOutDto(int Count);
    [HttpGet]
    public ActionResult<GetBoardsOutDto> Get()
    {
        return new GetBoardsOutDto(boardStorage.Count);
    }

    // 
    // This known security issue is now tracked in GitHub issue #9.
    // See: https://github.com/MikDal002/ZTR.BoardGame/issues/9
    //
    // This endpoint contains security issue that the server will be invoking not checked address.
    // It should be handled in production version of application. As it is a PoC we can leave it as is.
    //
    [HttpPost]
    public IActionResult Post([FromQuery] string responseAddress)
    {
        if (string.IsNullOrWhiteSpace(responseAddress))
        {
            logger.LogWarning("Request received without a remote IP address.");
            return BadRequest("Could not determine remote IP address.");
        }

        var boardIpAddress = new Uri(responseAddress);

        var _ = logger.BeginScopeWith(("BoardIpAddress", boardIpAddress.ToString()));

        logger.LogInformation("Received hello from board");
        console.MarkupLine($"Received hello from board {boardIpAddress}");

        boardStorage.Add(boardIpAddress);
        return Ok();
    }

    [HttpPost("game/status")]
    public IActionResult PostGameStatus([FromQuery] string responseAddress, [FromQuery] TimeSpan result)
    {
        gameService.RecordResults(new Board(new Uri(responseAddress)), new GameResult(result));
        logger.LogInformation("Received game status from board: {Board}", responseAddress);
        return Ok();
    }
}
