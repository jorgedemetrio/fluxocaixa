using FluentAssertions;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Domain.Events;
using FluxoCaixa.Shared.Kernel;
using Xunit;

namespace FluxoCaixa.Lancamentos.UnitTests.Domain;

public class LancamentoTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [Fact]
    public void Criar_ComDadosValidos_DeveCriarLancamento()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        var lancamento = Lancamento.Criar(TipoLancamento.Credito, 150.00m, "Venda produto X", data, UsuarioId);

        lancamento.Tipo.Should().Be(TipoLancamento.Credito);
        lancamento.Valor.Quantia.Should().Be(150.00m);
        lancamento.Descricao.Should().Be("Venda produto X");
        lancamento.Status.Should().Be(StatusLancamento.Ativo);
        lancamento.UsuarioId.Should().Be(UsuarioId);
        lancamento.DataFutura.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Criar_ComValorInvalido_DeveLancarDomainException(decimal valor)
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        var act = () => Lancamento.Criar(TipoLancamento.Credito, valor, "Teste", data, UsuarioId);
        act.Should().Throw<DomainException>().WithMessage("*positivo*");
    }

    [Fact]
    public void Criar_ComValorAcimaDoLimite_DeveLancarDomainException()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        var act = () => Lancamento.Criar(TipoLancamento.Credito, 10_000_000m, "Teste", data, UsuarioId);
        act.Should().Throw<DomainException>().WithMessage("*limite*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")] // menos de 3 chars
    public void Criar_ComDescricaoInvalida_DeveLancarException(string descricao)
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        var act = () => Lancamento.Criar(TipoLancamento.Credito, 100m, descricao, data, UsuarioId);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Criar_ComDataFutura_DeveMarcarDataFuturaComoTrue()
    {
        var dataFutura = DateOnly.FromDateTime(DateTime.Today.AddDays(5));
        var lancamento = Lancamento.Criar(TipoLancamento.Credito, 100m, "Venda futura", dataFutura, UsuarioId);
        lancamento.DataFutura.Should().BeTrue();
    }

    [Fact]
    public void Criar_DeveAdicionarDomainEvent()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        var lancamento = Lancamento.Criar(TipoLancamento.Credito, 100m, "Teste", data, UsuarioId);

        lancamento.DomainEvents.Should().HaveCount(1);
        lancamento.DomainEvents[0].Should().BeOfType<LancamentoCriadoEvent>();
    }

    [Fact]
    public void Cancelar_ComDadosValidos_DeveCancelarLancamento()
    {
        var lancamento = CriarLancamentoValido();
        var canceladoPor = Guid.NewGuid();

        lancamento.Cancelar("Produto devolvido pelo cliente", canceladoPor);

        lancamento.Status.Should().Be(StatusLancamento.Cancelado);
        lancamento.MotivoCancelamento.Should().Be("Produto devolvido pelo cliente");
        lancamento.CanceladoEm.Should().NotBeNull();
        lancamento.CanceladoPor.Should().Be(canceladoPor);
    }

    [Fact]
    public void Cancelar_LancamentoJaCancelado_DeveLancarDomainException()
    {
        var lancamento = CriarLancamentoValido();
        lancamento.Cancelar("Primeiro cancelamento", Guid.NewGuid());

        var act = () => lancamento.Cancelar("Segundo cancelamento", Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*já está cancelado*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("curto")] // menos de 10 chars
    public void Cancelar_ComMotivoInvalido_DeveLancarDomainException(string motivo)
    {
        var lancamento = CriarLancamentoValido();
        var act = () => lancamento.Cancelar(motivo, Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*Motivo*");
    }

    [Fact]
    public void Cancelar_DeveAdicionarDomainEventDeCancelamento()
    {
        var lancamento = CriarLancamentoValido();
        lancamento.ClearDomainEvents();

        lancamento.Cancelar("Cancelamento por motivo válido", Guid.NewGuid());

        lancamento.DomainEvents.Should().HaveCount(1);
        lancamento.DomainEvents[0].Should().BeOfType<LancamentoCanceladoEvent>();
    }

    [Fact]
    public void ClearDomainEvents_DeveRemoverTodosOsEventos()
    {
        var lancamento = CriarLancamentoValido();
        lancamento.DomainEvents.Should().HaveCount(1);

        lancamento.ClearDomainEvents();

        lancamento.DomainEvents.Should().BeEmpty();
    }

    private static Lancamento CriarLancamentoValido() =>
        Lancamento.Criar(TipoLancamento.Credito, 500m, "Venda de produto", DateOnly.FromDateTime(DateTime.Today), UsuarioId);
}
