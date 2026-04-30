using FluxoCaixa.Lancamentos.Application.Commands.CancelLancamento;
using FluxoCaixa.Lancamentos.Application.Commands.CreateLancamento;
using FluxoCaixa.Lancamentos.Application.DTOs;
using FluxoCaixa.Lancamentos.Application.Queries.GetLancamentoById;
using FluxoCaixa.Lancamentos.Application.Queries.GetLancamentos;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FluxoCaixa.Lancamentos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LancamentosController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(LancamentoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar(
        [FromBody] CreateLancamentoRequest req,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
        CancellationToken ct)
    {
        var usuarioId = ObterUsuarioId();
        var cmd = new CreateLancamentoCommand(
            req.Tipo, req.Valor, req.Descricao, req.Data,
            usuarioId, req.CategoriaId, idempotencyKey);

        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(ObterPorId), new { id = result.Value!.Id }, result.Value)
            : HandleError(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LancamentoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetLancamentoByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LancamentoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] TipoLancamento? tipo,
        [FromQuery] StatusLancamento? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (pageSize > 100) pageSize = 100;
        var query = new GetLancamentosQuery(dataInicio, dataFim, tipo, status, page, pageSize);
        var result = await mediator.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(LancamentoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancelar(
        Guid id, [FromBody] CancelLancamentoRequest req, CancellationToken ct)
    {
        var usuarioId = ObterUsuarioId();
        var isGestor = User.IsInRole("Gestor") || User.IsInRole("Admin");

        var cmd = new CancelLancamentoCommand(id, req.Motivo, usuarioId, isGestor);
        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result);
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy", service = "lancamentos", timestamp = DateTime.UtcNow });

    private Guid ObterUsuarioId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(sub))
            throw new UnauthorizedAccessException("Usuário não autenticado");
        return Guid.Parse(sub);
    }

    private IActionResult HandleError<T>(Result<T> result) =>
        result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { error = result.Error }),
            ErrorType.Validation => UnprocessableEntity(new { error = result.Error }),
            ErrorType.Unauthorized => Forbid(),
            ErrorType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
}

public record CreateLancamentoRequest(
    TipoLancamento Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    Guid? CategoriaId = null
);

public record CancelLancamentoRequest(string Motivo);
