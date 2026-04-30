using FluxoCaixa.Consolidado.Domain.Entities;
using FluxoCaixa.Consolidado.Domain.Repositories;
using FluxoCaixa.Consolidado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

public class ConsolidadoRepository(ConsolidadoDbContext db) : IConsolidadoRepository
{
    public async Task<ConsolidadoDiario?> GetByDataAsync(DateOnly data, CancellationToken ct = default) =>
        await db.ConsolidadosDiarios.FirstOrDefaultAsync(x => x.Data == data, ct);

    public async Task<IReadOnlyList<ConsolidadoDiario>> GetByPeriodoAsync(
        DateOnly dataInicio, DateOnly dataFim, CancellationToken ct = default) =>
        await db.ConsolidadosDiarios
            .Where(x => x.Data >= dataInicio && x.Data <= dataFim)
            .OrderBy(x => x.Data)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<ConsolidadoDiario?> GetUltimoDiaAnteriorAsync(DateOnly data, CancellationToken ct = default) =>
        await db.ConsolidadosDiarios
            .Where(x => x.Data < data)
            .OrderByDescending(x => x.Data)
            .FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(ConsolidadoDiario consolidado, CancellationToken ct = default)
    {
        var existente = await db.ConsolidadosDiarios
            .FirstOrDefaultAsync(x => x.Id == consolidado.Id, ct);

        if (existente is null)
            await db.ConsolidadosDiarios.AddAsync(consolidado, ct);
        else
            db.Entry(existente).CurrentValues.SetValues(consolidado);

        await db.SaveChangesAsync(ct);
    }
}
