using FluentAssertions;
using FluxoCaixa.Lancamentos.Application.Abstractions;
using FluxoCaixa.Lancamentos.Application.Commands.CreateLancamento;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Domain.Repositories;
using Moq;
using Xunit;

namespace FluxoCaixa.Lancamentos.UnitTests.Application;

public class CreateLancamentoHandlerTests
{
    private readonly Mock<ILancamentoRepository> _repositoryMock = new();
    private readonly Mock<IMessagePublisher> _publisherMock = new();
    private readonly CreateLancamentoHandler _handler;

    public CreateLancamentoHandlerTests()
    {
        _handler = new CreateLancamentoHandler(_repositoryMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DeveCriarLancamentoEPublicarEvento()
    {
        var usuarioId = Guid.NewGuid();
        var cmd = new CreateLancamentoCommand(
            TipoLancamento.Credito, 500m, "Venda de produto",
            DateOnly.FromDateTime(DateTime.Today), usuarioId);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Tipo.Should().Be("CREDITO");
        result.Value.Valor.Should().Be(500m);
        result.Value.Status.Should().Be("ATIVO");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComIdempotencyKeyExistente_DeveRetornarLancamentoExistente()
    {
        var idempotencyKey = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var lancamentoExistente = Lancamento.Criar(
            TipoLancamento.Credito, 200m, "Venda existente",
            DateOnly.FromDateTime(DateTime.Today), usuarioId, null, idempotencyKey);

        _repositoryMock.Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamentoExistente);

        var cmd = new CreateLancamentoCommand(
            TipoLancamento.Credito, 200m, "Venda existente",
            DateOnly.FromDateTime(DateTime.Today), usuarioId, null, idempotencyKey);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(lancamentoExistente.Id);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DevePublicarEventoAposGravar()
    {
        var ordemPublicacao = new List<string>();
        var usuarioId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Lancamento>(), It.IsAny<CancellationToken>()))
            .Callback(() => ordemPublicacao.Add("salvar"))
            .Returns(Task.CompletedTask);

        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback(() => ordemPublicacao.Add("publicar"))
            .Returns(Task.CompletedTask);

        var cmd = new CreateLancamentoCommand(
            TipoLancamento.Debito, 100m, "Despesa de teste",
            DateOnly.FromDateTime(DateTime.Today), usuarioId);

        await _handler.Handle(cmd, CancellationToken.None);

        ordemPublicacao.Should().ContainInOrder("salvar", "publicar");
    }
}
