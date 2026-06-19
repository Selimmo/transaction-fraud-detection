using Amazon.SQS;

namespace TransactionFraudDetection.Messaging;

public class SqsQueueResolver(IAmazonSQS sqsClient, string queueName)
{
    private string? _queueUrl;

    public async Task<string> GetQueueUrlAsync(CancellationToken cancellationToken = default) =>
        _queueUrl ??= (await sqsClient.CreateQueueAsync(queueName, cancellationToken)).QueueUrl;
}
