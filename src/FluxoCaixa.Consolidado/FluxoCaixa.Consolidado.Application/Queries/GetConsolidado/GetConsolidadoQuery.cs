using FluxoCaixa.Consolidado.Application.DTOs;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Consolidado.Application.Queries.GetConsolidado;

public record GetConsolidadoQuery(DateOnly Data) : IRequest<Result<ConsolidadoDto>>;

public record GetHistoricoConsolidadoQuery(
    DateOnly DataInicio,
    DateOnly DataFim
) : IRequest<Result<HistoricoConsolidadoDto>>;
