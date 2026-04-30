using Amazon.SQS;
using Amazon.SQS.Model;
using FluxoCaixa.Consolidado.Application.EventHandlers;
using System.Text.Json;

namespace FluxoCaixa.Consolidado.API.Workers;

public class SqsConsumerWorker(
    IAmazonSQS sqsClient,
    LancamentoEventHandler eventHandler,
    IConfiguration configuration,
    ILogger<SqsConsumerWorker> logger
) : BackgroundService
{
    private readonly string _urlFila = configuration["Sqs:QueueUrl"]!;
    private static readonly JsonSerializerOptions OpcoesJson =
        new() { PropertyNameCaseInsensitive = true };
    private const int AtrasoAposErroMs = 5_000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS Consumer iniciado. Fila: {UrlFila}", _urlFila);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _urlFila,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20,
                    MessageAttributeNames = ["EventType"]
                }, stoppingToken);

                foreach (var message in response.Messages)
                    await ProcessarMensagem(message, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no SQS Consumer. Aguardando antes de tentar novamente");
                await Task.Delay(AtrasoAposErroMs, stoppingToken);
            }
        }
    }

    private async Task ProcessarMensagem(Message message, CancellationToken ct)
    {
        var tipoEvento = message.MessageAttributes.TryGetValue("EventType", out var atributo)
            ? atributo.StringValue : null;

        try
        {
            switch (tipoEvento)
            {
                case "LancamentoCriadoEvent":
                    var criado = JsonSerializer.Deserialize<LancamentoCriadoIntegrationEvent>(message.Body, OpcoesJson)!;
                    await eventHandler.HandleCriado(criado, ct);
                    break;

                case "LancamentoCanceladoEvent":
                    var cancelado = JsonSerializer.Deserialize<LancamentoCanceladoIntegrationEvent>(message.Body, OpcoesJson)!;
                    await eventHandler.HandleCancelado(cancelado, ct);
                    break;

                default:
                    logger.LogWarning("Tipo de evento desconhecido: {TipoEvento}", tipoEvento);
                    break;
            }

            await sqsClient.DeleteMessageAsync(_urlFila, message.ReceiptHandle, ct);
            logger.LogInformation("Mensagem {MessageId} processada com sucesso", message.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar mensagem {MessageId}", message.MessageId);
            // Mensagem volta automaticamente para a fila após o Visibility Timeout
        }
    }
}
