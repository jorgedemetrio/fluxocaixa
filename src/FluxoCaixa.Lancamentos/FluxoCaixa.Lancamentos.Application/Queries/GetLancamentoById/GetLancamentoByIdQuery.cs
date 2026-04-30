using FluxoCaixa.Lancamentos.Application.DTOs;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Queries.GetLancamentoById;

public record GetLancamentoByIdQuery(Guid Id) : IRequest<Result<LancamentoDto>>;
