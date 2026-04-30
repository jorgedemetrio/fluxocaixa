using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Shared.Kernel;

namespace FluxoCaixa.Lancamentos.Domain.Events;

public record LancamentoCriadoEvent(
    Guid LancamentoId,
    TipoLancamento Tipo,
    decimal Valor,
    DateOnly Data,
    Guid UsuarioId
) : DomainEvent;

public record LancamentoCanceladoEvent(
    Guid LancamentoId,
    TipoLancamento Tipo,
    decimal Valor,
    DateOnly Data,
    Guid UsuarioId,
    string Motivo
) : DomainEvent;
