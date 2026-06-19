using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Messaging;

namespace TransactionFraudDetection.Api;

public class SqsFindingsPublisher(IAmazonSQS sqsClient, SqsQueueResolver queueResolver)
{
    public async Task PublishAsync(FraudCheckNotification notification)
    {
        var queueUrl = await queueResolver.GetQueueUrlAsync();

        await sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(notification),
        });
    }
}
