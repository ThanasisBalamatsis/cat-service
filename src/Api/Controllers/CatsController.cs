using Application.Cats.Fetch;
using Application.Cats.Get;
using Application.Cats.GetAll;
using Application.Cats.Requests;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiVersion(1)]
[ApiController]
public sealed class CatsController : ControllerBase
{
    private readonly ISender _sender;

    public CatsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost(ApiEndpoints.Cats.Fetch)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Fetch(CancellationToken cancellationToken)
    {
        var command = new FetchCatsCommand();
        var response = await _sender.Send(command, cancellationToken);

        return AcceptedAtAction(
            actionName: nameof(JobsController.Get),
            controllerName: "Jobs",
            routeValues: new { id = response.JobId, v = "1" },
            value: response);
    }

    [HttpGet(ApiEndpoints.Cats.Get)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromRoute] int id, 
        CancellationToken cancellationToken)
    {
        var query = new GetCatQuery { Id = id };

        var response = await _sender.Send(query, cancellationToken);

        return response is null 
            ? NotFound() 
            : Ok(response);
    }

    [HttpGet(ApiEndpoints.Cats.GetAll)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllCatsRequest request, 
        CancellationToken cancellationToken)
    {
        if (request.Page < 1)
        {
            return BadRequest("Page must be greater than 0");
        }

        if (request.PageSize < 1)
        {
            return BadRequest("Page size must be greater than 0");
        }

        if (request.PageSize > 100)
        {
            return BadRequest("Page size must be less than or equal to 100");
        } 

        var query = new GetAllCatsQuery
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Tag = request.Tag
        };

        var response = await _sender.Send(query, cancellationToken);
        return Ok(response);
    }
}
