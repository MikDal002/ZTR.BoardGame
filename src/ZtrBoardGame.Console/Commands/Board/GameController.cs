using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

[ApiController, Route("api/board/game")]
public class GameController(IBoardGameStatusStorage boardGameStatusStorage) : ControllerBase
{
    [HttpPost]
    public IActionResult Post()
    {
        var list = Enumerable.Range(0, 16).OrderBy(_ => Random.Shared.Next()).Take(4).ToList();
        boardGameStatusStorage.FieldOrder = new FieldOrder(list);
        boardGameStatusStorage.StartGameRequested = true;
        return Ok();
    }
}
