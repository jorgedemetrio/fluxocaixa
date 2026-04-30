using FluxoCaixa.Lancamentos.Application.DTOs;
using FluxoCaixa.Lancamentos.Domain.Repositories;
using FluxoCaixa.Shared.Kernel;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Queries.GetLancamentoById;

public class GetLancamentoByIdHandler(ILancamentoRepository repository)
    : IRequestHandler<GetLancamentoByIdQuery, Result<LancamentoDto>>
{
    public async Task<Result<LancamentoDto>> Handle(
        GetLancamentoByIdQuery query, CancellationToken ct)
    {
        var lancamento = await repository.GetByIdAsync(query.Id, ct);

        return lancamento is null
            ? Result<LancamentoDto>.NotFound("Lançamento")
            : Result<LancamentoDto>.Success(LancamentoDto.From(lancamento));
    }
}
