namespace FluxoCaixa.Lancamentos.Application.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
