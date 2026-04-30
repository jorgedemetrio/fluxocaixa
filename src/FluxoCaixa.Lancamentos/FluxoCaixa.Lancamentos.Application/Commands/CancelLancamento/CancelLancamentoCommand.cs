using FluxoCaixa.Lancamentos.Application.DTOs;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Commands.CancelLancamento;

public record CancelLancamentoCommand(
    Guid Id,
    string Motivo,
    Guid UsuarioId,
    bool IsGestor
) : IRequest<Result<LancamentoDto>>;
