using Application.Jobs.Get;
using Application.Jobs.Responses;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

/// <summary>
/// Tracks the status of background cat fetch jobs.
/// </summary>
[ApiVersion(1)]
[ApiController]
public sealed class JobsController : ControllerBase
{
    private readonly ISender _sender;

    public JobsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets the status of a cat fetch job.
    /// </summary>
    /// <param name="id">The job ID returned from the fetch endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Job status and details.</returns>
    /// <response code="200">Job found.</response>
    /// <response code="404">Job not found.</response>
    [HttpGet(ApiEndpoints.Jobs.Get)]
    [ProducesResponseType(typeof(JobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var query = new GetJobQuery { Id = id };

        var response = await _sender.Send(query, cancellationToken);

        return response is null
            ? NotFound()
            : Ok(response);
    }
}
