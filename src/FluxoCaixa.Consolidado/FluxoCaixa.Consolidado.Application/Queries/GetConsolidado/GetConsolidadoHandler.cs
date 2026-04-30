using FluxoCaixa.Consolidado.Application.Abstractions;
using FluxoCaixa.Consolidado.Application.DTOs;
using FluxoCaixa.Consolidado.Domain.Repositories;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Consolidado.Application.Queries.GetConsolidado;

public class GetConsolidadoHandler(
    IConsolidadoRepository repository,
    ICacheService cache
) : IRequestHandler<GetConsolidadoQuery, Result<ConsolidadoDto>>,
    IRequestHandler<GetHistoricoConsolidadoQuery, Result<HistoricoConsolidadoDto>>
{
    public async Task<Result<ConsolidadoDto>> Handle(
        GetConsolidadoQuery query, CancellationToken ct)
    {
        var chaveCache = $"consolidado:{query.Data:yyyy-MM-dd}";
        var emCache = await cache.GetAsync<ConsolidadoDto>(chaveCache, ct);
        if (emCache is not null)
            return Result<ConsolidadoDto>.Success(emCache);

        var consolidado = await repository.GetByDataAsync(query.Data, ct);
        var dto = consolidado is not null
            ? ConsolidadoDto.From(consolidado)
            : ConsolidadoDto.Vazio(query.Data);

        await cache.SetAsync(chaveCache, dto, TimeSpan.FromMinutes(5), ct);
        return Result<ConsolidadoDto>.Success(dto);
    }

    public async Task<Result<HistoricoConsolidadoDto>> Handle(
        GetHistoricoConsolidadoQuery query, CancellationToken ct)
    {
        var consolidados = await repository.GetByPeriodoAsync(query.DataInicio, query.DataFim, ct);
        var dtos = consolidados.Select(ConsolidadoDto.From).ToList().AsReadOnly();

        var historico = new HistoricoConsolidadoDto(
            query.DataInicio, query.DataFim, dtos,
            dtos.Sum(x => x.TotalCreditos),
            dtos.Sum(x => x.TotalDebitos),
            dtos.Sum(x => x.SaldoFinal));

        return Result<HistoricoConsolidadoDto>.Success(historico);
    }
}
