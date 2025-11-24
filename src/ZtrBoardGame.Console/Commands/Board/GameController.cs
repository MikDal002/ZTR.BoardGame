using Microsoft.AspNetCore.Mvc;

namespace ZtrBoardGame.Console.Commands.Board;

[ApiController, Route("api/board/game")]
public class GameController(IBoardGameStatusStorage boardGameStatusStorage) : ControllerBase
{
    [HttpPost]
    public IActionResult Post()
    {
        boardGameStatusStorage.StartGameRequested = true;
        return Ok();
    }
}
