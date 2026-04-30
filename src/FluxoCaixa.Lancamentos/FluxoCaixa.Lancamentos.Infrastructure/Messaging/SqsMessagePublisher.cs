using Amazon.SQS;
using Amazon.SQS.Model;
using FluxoCaixa.Lancamentos.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FluxoCaixa.Lancamentos.Infrastructure.Messaging;

public class SqsSettings
{
    public string QueueUrl { get; set; } = null!;
    public string Region { get; set; } = "us-east-1";
}

public class SqsMessagePublisher(
    IAmazonSQS sqsClient,
    IOptions<SqsSettings> settings,
    ILogger<SqsMessagePublisher> logger
) : IMessagePublisher
{
    private readonly SqsSettings _settings = settings.Value;

    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        var eventType = message.GetType().Name;
        var payload = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var request = new SendMessageRequest
        {
            QueueUrl = _settings.QueueUrl,
            MessageBody = payload,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new() { DataType = "String", StringValue = eventType }
            }
        };

        try
        {
            await sqsClient.SendMessageAsync(request, ct);
            logger.LogInformation("Evento {EventType} publicado no SQS com sucesso", eventType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao publicar evento {EventType} no SQS", eventType);
            throw;
        }
    }
}
