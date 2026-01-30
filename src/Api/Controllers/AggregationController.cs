using Application.Features.Aggregation;
using Application.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AggregationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AggregationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<AggregationResponse>> Get([FromQuery] AggregationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAggregatedDataQuery(request), ct);
        return Ok(result);
    }
}
