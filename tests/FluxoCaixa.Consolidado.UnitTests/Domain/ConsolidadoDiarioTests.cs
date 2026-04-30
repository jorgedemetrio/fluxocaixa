using FluentAssertions;
using FluxoCaixa.Consolidado.Domain.Entities;
using FluxoCaixa.Shared.Kernel;
using Xunit;

namespace FluxoCaixa.Consolidado.UnitTests.Domain;

public class ConsolidadoDiarioTests
{
    private readonly DateOnly _hoje = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void Criar_DeveInicializarComValoresZerados()
    {
        var consolidado = ConsolidadoDiario.Criar(_hoje);

        consolidado.Data.Should().Be(_hoje);
        consolidado.TotalCreditos.Should().Be(0);
        consolidado.TotalDebitos.Should().Be(0);
        consolidado.SaldoFinal.Should().Be(0);
        consolidado.QtdLancamentos.Should().Be(0);
        consolidado.Versao.Should().Be(1);
    }

    [Fact]
    public void Criar_ComSaldoAcumuladoAnterior_DeveHerdarSaldo()
    {
        var consolidado = ConsolidadoDiario.Criar(_hoje, saldoAcumuladoAnterior: 1000m);
        consolidado.SaldoAcumulado.Should().Be(1000m);
    }

    [Fact]
    public void AplicarLancamento_Credito_DeveIncrementarTotalCreditos()
    {
        var consolidado = ConsolidadoDiario.Criar(_hoje);
        consolidado.AplicarLancamento(TipoLancamentoEvento.Credito, 500m);

        consolidado.TotalCreditos.Should().Be(500m);
        consolidado.TotalDebitos.Should().Be(0m);
        consolidado.SaldoFinal.Should().Be(500m);
        consolidado.QtdLancamentos.Should().Be(1);
        consolidado.QtdCreditos.Should().Be(1);
        consolidado.Versao.Should().Be(2);
    }

    [Fact]
    public void AplicarLancamento_Debito_DeveIncrementarTotalDebitos()
    {
        var consolidado = ConsolidadoDiario.Criar(_hoje);
        consolidado.AplicarLancamento(TipoLancamentoEvento.Debito, 300m);

        consolidado.TotalDebitos.Should().Be(300m);
        consolidado.TotalCreditos.Should().Be(0m);
        consolidado.SaldoFinal.Should().Be(-300m);
        consolidado.QtdDebitos.Should().Be(1);
    }

    [Fact]
    public void AplicarMultiplosLancamentos_DeveCalcularSaldoCorretamente()
    {
        var consolidado = ConsolidadoDiario.Criar(_hoje);

        consolidado.AplicarLancamento(TipoLancamentoEvento.Credito, 1000m);
        consolidado.AplicarLancamento(TipoLancamentoEvento.Credito, 500m);
        consolidado.AplicarLancamento(TipoLancamentoEvento.Debito, 300m);
        consolidado.AplicarLancamento(TipoLancamentoEvento.Debito, 200m);

        consolidado.TotalCreditos.Should().Be(1500m);
        consolidado.TotalDebitos.Should().Be(500m);
        consolidado.SaldoFinal.Should().Be(1000m);
        consolidado.QtdLancamentos.Should().Be(4);
        consolidado.QtdCreditos.Should().Be(2);
        consolidado.QtdDebitos.Should().Be(2);
    }

    [Fact]
    public void AplicarLancamento_ComValorZero_DeveLancarDomainException()
    {
        var consolidado = ConsolidadoDiario.Criar(_hoje);
        var act = () => consolidado.AplicarLancamento(TipoLancamentoEvento.Credito, 0m);
        act.Should().Throw<DomainException>().WithMessage("*positivo*");
    }

    [Fact]
    public void ReverterLancamento_Credito_DeveDecrementarTotalCreditos()
    {
        var consolidado = ConsolidadoDiario.Criar(_hoje);
        consolidado.AplicarLancamento(TipoLancamentoEvento.Credito, 500m);
        consolidado.ReverterLancamento(TipoLancamentoEvento.Credito, 500m);

        consolidado.TotalCreditos.Should().Be(0m);
        consolidado.QtdLancamentos.Should().Be(0);
        consolidado.QtdCreditos.Should().Be(0);
    }

    [Fact]
    public void SaldoPodeSerNegativo_NaoDeveLancarException()
    {
        var consolidado = ConsolidadoDiario.Criar(_hoje);
        var act = () => consolidado.AplicarLancamento(TipoLancamentoEvento.Debito, 9_999_999m);
        act.Should().NotThrow();
        consolidado.SaldoFinal.Should().BeLessThan(0);
    }

    [Fact]
    public void AtualizarSaldoAcumulado_DeveCalcularCorreto()
    {
        var consolidado = ConsolidadoDiario.Criar(_hoje);
        consolidado.AplicarLancamento(TipoLancamentoEvento.Credito, 1000m);
        consolidado.AtualizarSaldoAcumulado(5000m);

        consolidado.SaldoAcumulado.Should().Be(6000m);
    }
}
