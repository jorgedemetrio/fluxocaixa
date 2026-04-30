using FluxoCaixa.Shared.Kernel;

namespace FluxoCaixa.Consolidado.Domain.Entities;

public enum TipoLancamentoEvento { Credito, Debito }

public class ConsolidadoDiario : AggregateRoot
{
    public DateOnly Data { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public decimal SaldoFinal => TotalCreditos - TotalDebitos;
    public decimal SaldoAcumulado { get; private set; }
    public int QtdLancamentos { get; private set; }
    public int QtdCreditos { get; private set; }
    public int QtdDebitos { get; private set; }
    public DateTime UltimaAtualizacao { get; private set; }
    public int Versao { get; private set; }

    private ConsolidadoDiario() { } // EF Core

    public static ConsolidadoDiario Criar(DateOnly data, decimal saldoAcumuladoAnterior = 0)
    {
        return new ConsolidadoDiario
        {
            Id = Guid.NewGuid(),
            Data = data,
            TotalCreditos = 0,
            TotalDebitos = 0,
            SaldoAcumulado = saldoAcumuladoAnterior,
            QtdLancamentos = 0,
            QtdCreditos = 0,
            QtdDebitos = 0,
            UltimaAtualizacao = DateTime.UtcNow,
            Versao = 1,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };
    }

    public void AplicarLancamento(TipoLancamentoEvento tipo, decimal valor)
    {
        if (valor <= 0) throw new DomainException("Valor do lançamento deve ser positivo");

        if (tipo == TipoLancamentoEvento.Credito)
        {
            TotalCreditos += valor;
            QtdCreditos++;
            SaldoAcumulado += valor;
        }
        else
        {
            TotalDebitos += valor;
            QtdDebitos++;
            SaldoAcumulado -= valor;
        }

        QtdLancamentos++;
        UltimaAtualizacao = DateTime.UtcNow;
        Versao++;
        SetAtualizado();
    }

    public void ReverterLancamento(TipoLancamentoEvento tipo, decimal valor)
    {
        // Math.Max como defesa: eventos fora de ordem não devem deixar totais negativos
        if (tipo == TipoLancamentoEvento.Credito)
        {
            TotalCreditos = Math.Max(0, TotalCreditos - valor);
            QtdCreditos = Math.Max(0, QtdCreditos - 1);
            SaldoAcumulado -= valor;
        }
        else
        {
            TotalDebitos = Math.Max(0, TotalDebitos - valor);
            QtdDebitos = Math.Max(0, QtdDebitos - 1);
            SaldoAcumulado += valor;
        }

        QtdLancamentos = Math.Max(0, QtdLancamentos - 1);
        UltimaAtualizacao = DateTime.UtcNow;
        Versao++;
        SetAtualizado();
    }

    public void AtualizarSaldoAcumulado(decimal saldoAcumuladoAnterior)
    {
        SaldoAcumulado = saldoAcumuladoAnterior + SaldoFinal;
        SetAtualizado();
    }
}
