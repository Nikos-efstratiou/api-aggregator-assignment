using Application.Features.Stats;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StatsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderStatsDto>>> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApiStatsQuery(), ct);
        return Ok(result);
    }
}
