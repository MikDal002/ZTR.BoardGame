using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using ZtrBoardGame.Server.Commons.Online;

namespace ZtrBoardGame.Console.Commands.Board.Online;

public class GameStartRequestValidator : AbstractValidator<GameStartRequest>
{
    public GameStartRequestValidator()
    {
        RuleFor(x => x.Fields).NotEmpty().WithMessage("At least one field is required");
        RuleForEach(x => x.Fields).InclusiveBetween(0, 15).WithMessage("Field values must be between 0 and 15.");
        RuleFor(x => x.Fields).Must(fields => fields.Distinct().Count() == fields.Count).WithMessage("Field values must be unique.");
    }
}

[ApiController, Route("api/board/game")]
public class GameController(IBoardGameStatusStorage boardGameStatusStorage) : ControllerBase
{
    [HttpPost, Validate]
    public IActionResult Post(GameStartRequest gameRequest)
    {
        boardGameStatusStorage.Set(boardGameStatusStorage.Get().StartRequested(new(gameRequest.Fields)));
        return Ok();
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class ValidateAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.Count == 0)
        {
            return;
        }

        foreach (var argument in context.ActionArguments)
        {
            if (argument.Value == null)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    Errors = new[] { $"{argument.Key} cannot be null" }
                });
                return;
            }

            var type = typeof(IValidator<>).MakeGenericType(argument.Value.GetType());
            var validator = context.HttpContext.RequestServices
                    .GetService(type) as IValidator;

            if (validator != null)
            {
                var validationResult = validator.Validate(new
                    ValidationContext<object>(argument.Value));

                if (validationResult.IsValid)
                {
                    continue;
                }

                var errors = validationResult.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();

                context.Result = new BadRequestObjectResult(new { Errors = errors });
                return;
            }
            else
            {
                throw new InvalidOperationException($"Cannot find a validator for {type}");
            }
        }

        base.OnActionExecuting(context);
    }
}
