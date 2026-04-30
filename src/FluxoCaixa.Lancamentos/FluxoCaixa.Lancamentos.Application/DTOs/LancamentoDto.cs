using FluxoCaixa.Lancamentos.Domain.Entities;

namespace FluxoCaixa.Lancamentos.Application.DTOs;

public record LancamentoDto(
    Guid Id,
    string Tipo,
    decimal Valor,
    string Descricao,
    DateOnly Data,
    string Status,
    Guid UsuarioId,
    Guid? CategoriaId,
    bool DataFutura,
    string? MotivoCancelamento,
    DateTime? CanceladoEm,
    DateTime CriadoEm,
    DateTime AtualizadoEm
)
{
    public static LancamentoDto From(Lancamento l) => new(
        l.Id,
        l.Tipo.ToString().ToUpperInvariant(),
        l.Valor.Quantia,
        l.Descricao,
        l.Data,
        l.Status.ToString().ToUpperInvariant(),
        l.UsuarioId,
        l.CategoriaId,
        l.DataFutura,
        l.MotivoCancelamento,
        l.CanceladoEm,
        l.CriadoEm,
        l.AtualizadoEm
    );
}
