using FluentAssertions;
using FluxoCaixa.Lancamentos.Application.Abstractions;
using FluxoCaixa.Lancamentos.Application.Commands.CancelLancamento;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Domain.Repositories;
using FluxoCaixa.Shared.Kernel;
using Moq;
using Xunit;

namespace FluxoCaixa.Lancamentos.UnitTests.Application;

public class CancelLancamentoHandlerTests
{
    private readonly Mock<ILancamentoRepository> _repositoryMock = new();
    private readonly Mock<IMessagePublisher> _publisherMock = new();
    private readonly CancelLancamentoHandler _handler;
    private readonly Guid _usuarioId = Guid.NewGuid();

    public CancelLancamentoHandlerTests()
    {
        _handler = new CancelLancamentoHandler(_repositoryMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_LancamentoNaoEncontrado_DeveRetornarNotFound()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lancamento?)null);

        var cmd = new CancelLancamentoCommand(Guid.NewGuid(), "Motivo de cancelamento", _usuarioId, false);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_UsuarioDiferenteSemPermissao_DeveRetornarUnauthorized()
    {
        var outroUsuarioId = Guid.NewGuid();
        var lancamento = CriarLancamento(outroUsuarioId);

        _repositoryMock.Setup(r => r.GetByIdAsync(lancamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamento);

        var cmd = new CancelLancamentoCommand(lancamento.Id, "Motivo de cancelamento", _usuarioId, false);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_GestorCancelando_DevePermitirCancelamento()
    {
        var outroUsuarioId = Guid.NewGuid();
        var lancamento = CriarLancamento(outroUsuarioId);

        _repositoryMock.Setup(r => r.GetByIdAsync(lancamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamento);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new CancelLancamentoCommand(lancamento.Id, "Cancelado pelo gestor financeiro", _usuarioId, true);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELADO");
    }

    [Fact]
    public async Task Handle_ProprietarioCancelando_DeveCancelarComSucesso()
    {
        var lancamento = CriarLancamento(_usuarioId);

        _repositoryMock.Setup(r => r.GetByIdAsync(lancamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamento);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new CancelLancamentoCommand(lancamento.Id, "Venda cancelada pelo cliente", _usuarioId, false);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private Lancamento CriarLancamento(Guid usuarioId) =>
        Lancamento.Criar(TipoLancamento.Credito, 300m, "Venda de produto", DateOnly.FromDateTime(DateTime.Today), usuarioId);
}
