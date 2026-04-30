using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace FluxoCaixa.Integration.Tests;

/// <summary>
/// Testes de integração para a API de Lançamentos.
/// Requerem PostgreSQL e ElasticMQ disponíveis (via Docker Compose ou Testcontainers).
/// </summary>
[Collection("LancamentosApi")]
public class LancamentosApiIntegrationTests(WebApplicationFactory<global::Program> factory)
    : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task POST_Lancamentos_SemAutenticacao_DeveRetornar401()
    {
        var response = await _client.PostAsJsonAsync("/api/lancamentos", new
        {
            tipo = "CREDITO",
            valor = 100.00,
            descricao = "Teste",
            data = DateTime.Today.ToString("yyyy-MM-dd")
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Health_SemAutenticacao_DeveRetornar200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_LancamentosHealth_DeveRetornarStatus()
    {
        var response = await _client.GetAsync("/api/lancamentos/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
        content.Should().Contain("lancamentos");
    }
}
