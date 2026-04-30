using FluentAssertions;
using FluxoCaixa.Lancamentos.Domain.ValueObjects;
using FluxoCaixa.Shared.Kernel;
using Xunit;

namespace FluxoCaixa.Lancamentos.UnitTests.Domain;

public class DinheiroTests
{
    [Theory]
    [InlineData(0.01, 0.01)]
    [InlineData(100.00, 100.00)]
    [InlineData(9999999.99, 9999999.99)]
    [InlineData(1.999, 2.00)] // arredondamento
    [InlineData(1.001, 1.00)] // arredondamento
    public void Criar_ComValorValido_DeveArredondar(decimal entrada, decimal esperado)
    {
        var dinheiro = new Dinheiro(entrada);
        dinheiro.Quantia.Should().Be(esperado);
        dinheiro.Moeda.Should().Be("BRL");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Criar_ComValorNegativoOuZero_DeveLancarDomainException(decimal valor)
    {
        var act = () => new Dinheiro(valor);
        act.Should().Throw<DomainException>().WithMessage("*maior que zero*");
    }

    [Fact]
    public void Criar_ComValorAcimaDoLimite_DeveLancarDomainException()
    {
        var act = () => new Dinheiro(10_000_000m);
        act.Should().Throw<DomainException>().WithMessage("*limite*");
    }

    [Fact]
    public void ToString_DeveRetornarFormatoBRL()
    {
        var dinheiro = new Dinheiro(1500m);
        dinheiro.ToString().Should().Contain("1.500,00");
    }

    [Fact]
    public void Equality_DoisDinheirosComMesmoValor_DevemSerIguais()
    {
        var d1 = new Dinheiro(100m);
        var d2 = new Dinheiro(100m);
        d1.Should().Be(d2);
    }

    [Fact]
    public void Equality_DoisDinheirosComValoresDiferentes_NaoDevemSerIguais()
    {
        var d1 = new Dinheiro(100m);
        var d2 = new Dinheiro(200m);
        d1.Should().NotBe(d2);
    }
}
