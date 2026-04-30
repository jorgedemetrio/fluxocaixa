using FluxoCaixa.Lancamentos.Application.DTOs;
using FluxoCaixa.Lancamentos.Domain.Repositories;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Queries.GetLancamentos;

public class GetLancamentosHandler(ILancamentoRepository repository)
    : IRequestHandler<GetLancamentosQuery, Result<PagedResult<LancamentoDto>>>
{
    public async Task<Result<PagedResult<LancamentoDto>>> Handle(
        GetLancamentosQuery query, CancellationToken ct)
    {
        var dataFim = query.DataFim ?? query.DataInicio;

        var paged = await repository.GetByPeriodoAsync(
            query.DataInicio, dataFim, query.Tipo, query.Status,
            query.Page, query.PageSize, ct);

        var result = PagedResult<LancamentoDto>.Create(
            paged.Items.Select(LancamentoDto.From).ToList().AsReadOnly(),
            paged.TotalCount, paged.Page, paged.PageSize);

        return Result<PagedResult<LancamentoDto>>.Success(result);
    }
}
