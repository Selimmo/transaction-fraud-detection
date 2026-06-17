using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using TransactionFraudDetection.Domain;

namespace TransactionFraudDetection.Api;

public class SqsFindingsPublisher(IAmazonSQS sqsClient, string queueName)
{
    private string? _queueUrl;

    public async Task PublishAsync(FraudCheckNotification notification)
    {
        _queueUrl ??= (await sqsClient.CreateQueueAsync(queueName)).QueueUrl;

        await sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = JsonSerializer.Serialize(notification),
        });
    }
}
