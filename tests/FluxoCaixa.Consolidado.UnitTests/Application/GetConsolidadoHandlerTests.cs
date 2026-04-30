using FluentAssertions;
using FluxoCaixa.Consolidado.Application.Abstractions;
using FluxoCaixa.Consolidado.Application.DTOs;
using FluxoCaixa.Consolidado.Application.Queries.GetConsolidado;
using FluxoCaixa.Consolidado.Domain.Entities;
using FluxoCaixa.Consolidado.Domain.Repositories;
using Moq;
using Xunit;

namespace FluxoCaixa.Consolidado.UnitTests.Application;

public class GetConsolidadoHandlerTests
{
    private readonly Mock<IConsolidadoRepository> _repositoryMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly GetConsolidadoHandler _handler;
    private readonly DateOnly _hoje = DateOnly.FromDateTime(DateTime.Today);

    public GetConsolidadoHandlerTests()
    {
        _handler = new GetConsolidadoHandler(_repositoryMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_CacheHit_DeveRetornarDoCacheSemConsultarBanco()
    {
        var cachedDto = ConsolidadoDto.Vazio(_hoje);
        _cacheMock.Setup(c => c.GetAsync<ConsolidadoDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        var result = await _handler.Handle(new GetConsolidadoQuery(_hoje), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.GetByDataAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ConsolidadoDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_DeveConsultarBancoEGravarCache()
    {
        _cacheMock.Setup(c => c.GetAsync<ConsolidadoDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsolidadoDto?)null);

        var consolidado = ConsolidadoDiario.Criar(_hoje);
        consolidado.AplicarLancamento(TipoLancamentoEvento.Credito, 1000m);

        _repositoryMock.Setup(r => r.GetByDataAsync(_hoje, It.IsAny<CancellationToken>()))
            .ReturnsAsync(consolidado);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ConsolidadoDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new GetConsolidadoQuery(_hoje), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCreditos.Should().Be(1000m);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ConsolidadoDto>(), TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DiaSemConsolidado_DeveRetornarConsolidadoVazio()
    {
        _cacheMock.Setup(c => c.GetAsync<ConsolidadoDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsolidadoDto?)null);
        _repositoryMock.Setup(r => r.GetByDataAsync(_hoje, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsolidadoDiario?)null);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ConsolidadoDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(new GetConsolidadoQuery(_hoje), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCreditos.Should().Be(0);
        result.Value.SaldoFinal.Should().Be(0);
        result.Value.Data.Should().Be(_hoje);
    }

    [Fact]
    public async Task Handle_CacheKey_DeveUsarDataFormatada()
    {
        var data = new DateOnly(2024, 1, 15);
        string? capturedKey = null;

        _cacheMock.Setup(c => c.GetAsync<ConsolidadoDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => capturedKey = key)
            .ReturnsAsync((ConsolidadoDto?)null);
        _repositoryMock.Setup(r => r.GetByDataAsync(data, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsolidadoDiario?)null);
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ConsolidadoDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new GetConsolidadoQuery(data), CancellationToken.None);

        capturedKey.Should().Be("consolidado:2024-01-15");
    }
}
