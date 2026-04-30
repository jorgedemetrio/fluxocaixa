using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Domain.Repositories;
using FluxoCaixa.Lancamentos.Infrastructure.Persistence;
using FluxoCaixa.Shared.Kernel;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Lancamentos.Infrastructure.Repositories;

public class LancamentoRepository(LancamentosDbContext db) : ILancamentoRepository
{
    public async Task<Lancamento?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Lancamentos.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Lancamento?> GetByIdempotencyKeyAsync(Guid key, CancellationToken ct = default) =>
        await db.Lancamentos.FirstOrDefaultAsync(x => x.IdempotencyKey == key, ct);

    public async Task<PagedResult<Lancamento>> GetByPeriodoAsync(
        DateOnly dataInicio, DateOnly dataFim,
        TipoLancamento? tipo, StatusLancamento? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var lancamentos = db.Lancamentos.AsNoTracking()
            .Where(x => x.Data >= dataInicio && x.Data <= dataFim);

        if (tipo.HasValue)
            lancamentos = lancamentos.Where(x => x.Tipo == tipo.Value);

        if (status.HasValue)
            lancamentos = lancamentos.Where(x => x.Status == status.Value);

        var total = await lancamentos.CountAsync(ct);

        var itens = await lancamentos
            .OrderByDescending(x => x.Data)
            .ThenByDescending(x => x.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<Lancamento>.Create(itens.AsReadOnly(), total, page, pageSize);
    }

    public async Task AddAsync(Lancamento lancamento, CancellationToken ct = default)
    {
        await db.Lancamentos.AddAsync(lancamento, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Lancamento lancamento, CancellationToken ct = default)
    {
        db.Lancamentos.Update(lancamento);
        await db.SaveChangesAsync(ct);
    }
}
