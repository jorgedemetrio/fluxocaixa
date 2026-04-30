using FluxoCaixa.Shared.Kernel;

namespace FluxoCaixa.Lancamentos.Domain.ValueObjects;

public record Dinheiro
{
    public decimal Quantia { get; }
    public string Moeda { get; } = "BRL";

    public Dinheiro(decimal quantia)
    {
        if (quantia <= 0)
            throw new DomainException("Valor deve ser maior que zero");

        if (quantia > 9_999_999.99m)
            throw new DomainException("Valor excede o limite máximo permitido de R$ 9.999.999,99");

        Quantia = Math.Round(quantia, 2);
    }

    public override string ToString() => $"R$ {Quantia:N2}";
}
