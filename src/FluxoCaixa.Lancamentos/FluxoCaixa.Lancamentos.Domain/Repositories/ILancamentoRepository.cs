using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Shared.Kernel;

namespace FluxoCaixa.Lancamentos.Domain.Repositories;

public interface ILancamentoRepository
{
    Task<Lancamento?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Lancamento?> GetByIdempotencyKeyAsync(Guid key, CancellationToken ct = default);
    Task<PagedResult<Lancamento>> GetByPeriodoAsync(
        DateOnly dataInicio,
        DateOnly dataFim,
        TipoLancamento? tipo,
        StatusLancamento? status,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(Lancamento lancamento, CancellationToken ct = default);
    Task UpdateAsync(Lancamento lancamento, CancellationToken ct = default);
}
