using Application.Jobs.Get;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiVersion(1)]
[ApiController]
public sealed class JobsController : ControllerBase
{
    private readonly ISender _sender;

    public JobsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet(ApiEndpoints.Jobs.Get)]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
