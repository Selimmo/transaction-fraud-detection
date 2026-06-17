using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using TransactionFraudDetection.Domain;

namespace TransactionFraudDetection.Worker;

public class FindingsQueueListener(IAmazonSQS sqsClient, string queueName, FraudExplanationProcessor processor)
{
    private string? _queueUrl;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _queueUrl ??= (await sqsClient.CreateQueueAsync(queueName, cancellationToken)).QueueUrl;

        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10,
                },
                cancellationToken);

            foreach (var message in response.Messages)
            {
                var notification = JsonSerializer.Deserialize<FraudCheckNotification>(message.Body)
                    ?? throw new InvalidOperationException($"Could not deserialize message {message.MessageId}");

                await processor.ProcessAsync(notification);

                await sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, cancellationToken);
            }
        }
    }
}
