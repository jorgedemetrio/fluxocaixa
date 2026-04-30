using FluxoCaixa.Consolidado.Application.Abstractions;
using FluxoCaixa.Consolidado.Domain.Entities;
using FluxoCaixa.Consolidado.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FluxoCaixa.Consolidado.Application.EventHandlers;

public record LancamentoCriadoIntegrationEvent(
    Guid LancamentoId,
    string Tipo,
    decimal Valor,
    DateOnly Data,
    Guid UsuarioId
);

public record LancamentoCanceladoIntegrationEvent(
    Guid LancamentoId,
    string Tipo,
    decimal Valor,
    DateOnly Data,
    Guid UsuarioId,
    string Motivo
);

public class LancamentoEventHandler(
    IConsolidadoRepository repository,
    ICacheService cache,
    ILogger<LancamentoEventHandler> logger
)
{
    public async Task HandleCriado(LancamentoCriadoIntegrationEvent evt, CancellationToken ct = default)
    {
        logger.LogInformation("Processando LancamentoCriadoEvent: {LancamentoId}", evt.LancamentoId);

        var tipo = ParseTipo(evt.Tipo);
        var consolidado = await repository.GetByDataAsync(evt.Data, ct);

        if (consolidado is null)
        {
            var anterior = await repository.GetUltimoDiaAnteriorAsync(evt.Data, ct);
            var saldoAnterior = anterior?.SaldoAcumulado ?? 0;
            consolidado = ConsolidadoDiario.Criar(evt.Data, saldoAnterior);
        }

        consolidado.AplicarLancamento(tipo, evt.Valor);
        await repository.UpsertAsync(consolidado, ct);
        await cache.RemoveAsync($"consolidado:{evt.Data:yyyy-MM-dd}", ct);

        logger.LogInformation("Consolidado de {Data} atualizado. SaldoFinal: {Saldo}",
            evt.Data, consolidado.SaldoFinal);
    }

    public async Task HandleCancelado(LancamentoCanceladoIntegrationEvent evt, CancellationToken ct = default)
    {
        logger.LogInformation("Processando LancamentoCanceladoEvent: {LancamentoId}", evt.LancamentoId);

        var tipo = ParseTipo(evt.Tipo);
        var consolidado = await repository.GetByDataAsync(evt.Data, ct);

        if (consolidado is null)
        {
            logger.LogWarning("Consolidado não encontrado para data {Data} ao cancelar lançamento", evt.Data);
            return;
        }

        consolidado.ReverterLancamento(tipo, evt.Valor);
        await repository.UpsertAsync(consolidado, ct);
        await cache.RemoveAsync($"consolidado:{evt.Data:yyyy-MM-dd}", ct);
    }

    private static TipoLancamentoEvento ParseTipo(string tipo) =>
        string.Equals(tipo, "Credito", StringComparison.OrdinalIgnoreCase)
            ? TipoLancamentoEvento.Credito
            : TipoLancamentoEvento.Debito;
}
