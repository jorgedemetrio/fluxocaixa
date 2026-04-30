using FluxoCaixa.Consolidado.Application.DTOs;
using FluxoCaixa.Consolidado.Application.Queries.GetConsolidado;
using FluxoCaixa.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FluxoCaixa.Consolidado.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ConsolidadoController(IMediator mediator) : ControllerBase
{
    [HttpGet("{data}")]
    [ProducesResponseType(typeof(ConsolidadoDto), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 60, VaryByQueryKeys = ["data"])]
    public async Task<IActionResult> ObterPorData(DateOnly data, CancellationToken ct)
    {
        var result = await mediator.Send(new GetConsolidadoQuery(data), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result);
    }

    [HttpGet("historico")]
    [ProducesResponseType(typeof(HistoricoConsolidadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterHistorico(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim,
        CancellationToken ct)
    {
        if (dataFim < dataInicio)
            return BadRequest(new { error = "Data fim deve ser maior ou igual à data início" });

        var diferencaDias = dataFim.DayNumber - dataInicio.DayNumber;
        if (diferencaDias > 365)
            return BadRequest(new { error = "Período máximo permitido é de 365 dias" });

        var result = await mediator.Send(new GetHistoricoConsolidadoQuery(dataInicio, dataFim), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result);
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() =>
        Ok(new { status = "healthy", service = "consolidado", timestamp = DateTime.UtcNow });

    private IActionResult HandleError<T>(Result<T> result) =>
        result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { error = result.Error }),
            ErrorType.Validation => UnprocessableEntity(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
}
