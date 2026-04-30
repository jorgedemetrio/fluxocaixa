using FluxoCaixa.Lancamentos.Domain.Events;
using FluxoCaixa.Lancamentos.Domain.ValueObjects;
using FluxoCaixa.Shared.Kernel;

namespace FluxoCaixa.Lancamentos.Domain.Entities;

public enum TipoLancamento { Credito, Debito }
public enum StatusLancamento { Ativo, Cancelado }

public class Lancamento : AggregateRoot
{
    public TipoLancamento Tipo { get; private set; }
    public Dinheiro Valor { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public DateOnly Data { get; private set; }
    public StatusLancamento Status { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid? CategoriaId { get; private set; }
    public bool DataFutura { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTime? CanceladoEm { get; private set; }
    public Guid? CanceladoPor { get; private set; }
    public Guid? IdempotencyKey { get; private set; }

    private Lancamento() { } // o EF Core precisa de construtor sem parâmetros para recriar a entidade do banco

    public static Lancamento Criar(
        TipoLancamento tipo,
        decimal valor,
        string descricao,
        DateOnly data,
        Guid usuarioId,
        Guid? categoriaId = null,
        Guid? idempotencyKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        if (descricao.Length < 3)
            throw new DomainException("Descrição deve ter no mínimo 3 caracteres");

        if (descricao.Length > 500)
            throw new DomainException("Descrição deve ter no máximo 500 caracteres");

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataFutura = data > hoje;

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            Tipo = tipo,
            Valor = new Dinheiro(valor),
            Descricao = descricao.Trim(),
            Data = data,
            Status = StatusLancamento.Ativo,
            UsuarioId = usuarioId,
            CategoriaId = categoriaId,
            DataFutura = dataFutura,
            IdempotencyKey = idempotencyKey,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };

        lancamento.AddDomainEvent(new LancamentoCriadoEvent(
            lancamento.Id, lancamento.Tipo, lancamento.Valor.Quantia,
            lancamento.Data, lancamento.UsuarioId));

        return lancamento;
    }

    public void Cancelar(string motivo, Guid canceladoPor)
    {
        if (Status == StatusLancamento.Cancelado)
            throw new DomainException("Lançamento já está cancelado e não pode ser reativado");

        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10)
            throw new DomainException("Motivo de cancelamento deve ter no mínimo 10 caracteres");

        Status = StatusLancamento.Cancelado;
        MotivoCancelamento = motivo.Trim();
        CanceladoEm = DateTime.UtcNow;
        CanceladoPor = canceladoPor;
        SetAtualizado();

        AddDomainEvent(new LancamentoCanceladoEvent(
            Id, Tipo, Valor.Quantia, Data, UsuarioId, motivo));
    }
}
