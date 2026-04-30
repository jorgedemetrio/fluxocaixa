using FluxoCaixa.Lancamentos.Application.DTOs;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Commands.CreateLancamento;

public record CreateLancamentoCommand(
    TipoLancamento Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    Guid UsuarioId,
    Guid? CategoriaId = null,
    Guid? IdempotencyKey = null
) : IRequest<Result<LancamentoDto>>;
