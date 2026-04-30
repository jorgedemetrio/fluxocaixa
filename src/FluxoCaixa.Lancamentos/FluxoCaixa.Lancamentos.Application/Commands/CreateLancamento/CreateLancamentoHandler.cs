using FluxoCaixa.Lancamentos.Application.Abstractions;
using FluxoCaixa.Lancamentos.Application.DTOs;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Domain.Repositories;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Commands.CreateLancamento;

public class CreateLancamentoHandler(
    ILancamentoRepository repository,
    IMessagePublisher publisher
) : IRequestHandler<CreateLancamentoCommand, Result<LancamentoDto>>
{
    public async Task<Result<LancamentoDto>> Handle(
        CreateLancamentoCommand cmd, CancellationToken ct)
    {
        if (cmd.IdempotencyKey.HasValue)
        {
            var existente = await repository.GetByIdempotencyKeyAsync(cmd.IdempotencyKey.Value, ct);
            if (existente is not null)
                return Result<LancamentoDto>.Success(LancamentoDto.From(existente));
        }

        var lancamento = Lancamento.Criar(
            cmd.Tipo, cmd.Valor, cmd.Descricao, cmd.Data,
            cmd.UsuarioId, cmd.CategoriaId, cmd.IdempotencyKey);

        await repository.AddAsync(lancamento, ct);

        foreach (var domainEvent in lancamento.DomainEvents)
            await publisher.PublishAsync(domainEvent, ct);

        lancamento.ClearDomainEvents();

        return Result<LancamentoDto>.Success(LancamentoDto.From(lancamento));
    }
}
