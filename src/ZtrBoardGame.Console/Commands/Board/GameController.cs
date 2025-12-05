using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using ZtrBoardGame.RaspberryPi;

namespace ZtrBoardGame.Console.Commands.Board;

[ApiController, Route("api/board/game")]
public class GameController(IBoardGameStatusStorage boardGameStatusStorage) : ControllerBase
{
    [HttpPost]
    public IActionResult Post()
    {
        // This needs to be recieved from PC
        boardGameStatusStorage.FieldOrder = new FieldOrder(new List<int> { 1, 2, 3, 4 }.OrderBy(_ => Random.Shared.Next()).ToList());
        boardGameStatusStorage.StartGameRequested = true;
        return Ok();
    }
}
