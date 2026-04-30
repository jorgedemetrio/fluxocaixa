using FluxoCaixa.Lancamentos.Application.DTOs;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Queries.GetLancamentos;

public record GetLancamentosQuery(
    DateOnly DataInicio,
    DateOnly? DataFim,
    TipoLancamento? Tipo,
    StatusLancamento? Status,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<LancamentoDto>>>;
