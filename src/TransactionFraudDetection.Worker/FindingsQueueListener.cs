using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Messaging;

namespace TransactionFraudDetection.Worker;

public class FindingsQueueListener(IAmazonSQS sqsClient, SqsQueueResolver queueResolver, FraudExplanationProcessor processor)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var queueUrl = await queueResolver.GetQueueUrlAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10,
                },
                cancellationToken);

            foreach (var message in response.Messages)
            {
                var notification = JsonSerializer.Deserialize<FraudCheckNotification>(message.Body)
                    ?? throw new InvalidOperationException($"Could not deserialize message {message.MessageId}");

                await processor.ProcessAsync(notification);

                await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
            }
        }
    }
}
