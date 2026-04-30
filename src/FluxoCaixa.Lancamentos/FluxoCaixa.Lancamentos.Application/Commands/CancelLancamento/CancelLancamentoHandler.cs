using FluxoCaixa.Lancamentos.Application.Abstractions;
using FluxoCaixa.Lancamentos.Application.DTOs;
using FluxoCaixa.Lancamentos.Domain.Repositories;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Commands.CancelLancamento;

public class CancelLancamentoHandler(
    ILancamentoRepository repository,
    IMessagePublisher publisher
) : IRequestHandler<CancelLancamentoCommand, Result<LancamentoDto>>
{
    public async Task<Result<LancamentoDto>> Handle(
        CancelLancamentoCommand cmd, CancellationToken ct)
    {
        var lancamento = await repository.GetByIdAsync(cmd.Id, ct);

        if (lancamento is null)
            return Result<LancamentoDto>.NotFound(nameof(lancamento));

        if (!cmd.IsGestor && lancamento.UsuarioId != cmd.UsuarioId)
            return Result<LancamentoDto>.Unauthorized("Você não tem permissão para cancelar este lançamento");

        lancamento.Cancelar(cmd.Motivo, cmd.UsuarioId);
        await repository.UpdateAsync(lancamento, ct);

        foreach (var domainEvent in lancamento.DomainEvents)
            await publisher.PublishAsync(domainEvent, ct);

        lancamento.ClearDomainEvents();

        return Result<LancamentoDto>.Success(LancamentoDto.From(lancamento));
    }
}
