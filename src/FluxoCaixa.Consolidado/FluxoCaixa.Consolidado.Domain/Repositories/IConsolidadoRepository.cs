using FluxoCaixa.Consolidado.Domain.Entities;

namespace FluxoCaixa.Consolidado.Domain.Repositories;

public interface IConsolidadoRepository
{
    Task<ConsolidadoDiario?> GetByDataAsync(DateOnly data, CancellationToken ct = default);
    Task<IReadOnlyList<ConsolidadoDiario>> GetByPeriodoAsync(
        DateOnly dataInicio, DateOnly dataFim, CancellationToken ct = default);
    Task<ConsolidadoDiario?> GetUltimoDiaAnteriorAsync(DateOnly data, CancellationToken ct = default);
    Task UpsertAsync(ConsolidadoDiario consolidado, CancellationToken ct = default);
}
