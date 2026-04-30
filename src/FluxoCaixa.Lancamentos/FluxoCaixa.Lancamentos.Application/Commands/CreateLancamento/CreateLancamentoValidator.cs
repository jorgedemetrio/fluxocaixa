using FluentValidation;

namespace FluxoCaixa.Lancamentos.Application.Commands.CreateLancamento;

public class CreateLancamentoValidator : AbstractValidator<CreateLancamentoCommand>
{
    public CreateLancamentoValidator()
    {
        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero")
            .LessThanOrEqualTo(9_999_999.99m).WithMessage("Valor excede o limite máximo de R$ 9.999.999,99");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MinimumLength(3).WithMessage("Descrição deve ter no mínimo 3 caracteres")
            .MaximumLength(500).WithMessage("Descrição deve ter no máximo 500 caracteres");

        RuleFor(x => x.Data)
            .NotEmpty().WithMessage("Data é obrigatória");

        RuleFor(x => x.UsuarioId)
            .NotEmpty().WithMessage("UsuarioId é obrigatório");
    }
}
