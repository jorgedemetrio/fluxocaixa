using FluxoCaixa.Consolidado.Domain.Entities;

namespace FluxoCaixa.Consolidado.Application.DTOs;

public record ConsolidadoDto(
    DateOnly Data,
    decimal TotalCreditos,
    decimal TotalDebitos,
    decimal SaldoFinal,
    decimal SaldoAcumulado,
    int QtdLancamentos,
    int QtdCreditos,
    int QtdDebitos,
    DateTime UltimaAtualizacao
)
{
    public static ConsolidadoDto From(ConsolidadoDiario c) => new(
        c.Data, c.TotalCreditos, c.TotalDebitos,
        c.SaldoFinal, c.SaldoAcumulado,
        c.QtdLancamentos, c.QtdCreditos, c.QtdDebitos,
        c.UltimaAtualizacao);

    public static ConsolidadoDto Vazio(DateOnly data) => new(
        data, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow);
}

public record HistoricoConsolidadoDto(
    DateOnly PeriodoInicio,
    DateOnly PeriodoFim,
    IReadOnlyList<ConsolidadoDto> Consolidados,
    decimal TotalCreditosPeriodo,
    decimal TotalDebitosPeriodo,
    decimal SaldoLiquidoPeriodo
);
