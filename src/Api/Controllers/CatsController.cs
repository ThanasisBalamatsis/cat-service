using Application.Cats.Fetch;
using Application.Cats.Get;
using Application.Cats.GetAll;
using Application.Cats.Requests;
using Application.Cats.Responses;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

/// <summary>
/// Manages cat images fetched from The Cat API.
/// </summary>
[ApiVersion(1)]
[ApiController]
public sealed class CatsController : ControllerBase
{
    private readonly ISender _sender;

    public CatsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Triggers a background job to fetch cat images from The Cat API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created job details with a Location header to track progress.</returns>
    /// <response code="202">Job created successfully. Use the Location header to check status.</response>
    [HttpPost(ApiEndpoints.Cats.Fetch)]
    [ProducesResponseType(typeof(FetchCatsResponse), StatusCodes.Status202Accepted)]
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

    /// <summary>
    /// Gets a cat by its database ID.
    /// </summary>
    /// <param name="id">The database ID of the cat.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cat details including tags.</returns>
    /// <response code="200">Cat found.</response>
    /// <response code="404">Cat not found.</response>
    [HttpGet(ApiEndpoints.Cats.Get)]
    [ProducesResponseType(typeof(CatResponse), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Gets a paginated list of cats, optionally filtered by tag.
    /// </summary>
    /// <param name="request">Pagination and filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of cats.</returns>
    /// <response code="200">Cats retrieved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    [HttpGet(ApiEndpoints.Cats.GetAll)]
    [ProducesResponseType(typeof(CatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllCatsRequest request,
        CancellationToken cancellationToken)
    {
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
