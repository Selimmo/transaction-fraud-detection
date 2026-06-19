using System.Text.Json;
using Amazon.SQS;

namespace TransactionFraudDetection.Messaging;

public class SqsQueueResolver(IAmazonSQS sqsClient, string queueName, IAppEnvironment environment)
{
    private const int MaxReceiveCount = 5;

    private string? _queueUrl;

    public async Task<string> GetQueueUrlAsync(CancellationToken cancellationToken = default)
    {
        if (_queueUrl is not null)
        {
            return _queueUrl;
        }

        _queueUrl = environment.IsDevelopment
            ? await ProvisionDevelopmentQueueAsync(cancellationToken)
            : (await sqsClient.GetQueueUrlAsync(queueName, cancellationToken)).QueueUrl;

        return _queueUrl;
    }

    // Real deployments provision the queue and its dead-letter redrive policy via
    // infrastructure-as-code, and the app's IAM role typically can't call CreateQueue.
    // Locally there's no such pipeline, so the resolver provisions both idempotently
    // against LocalStack instead.
    private async Task<string> ProvisionDevelopmentQueueAsync(CancellationToken cancellationToken)
    {
        var deadLetterQueueUrl = (await sqsClient.CreateQueueAsync($"{queueName}-dlq", cancellationToken)).QueueUrl;
        var deadLetterQueueArn = (await sqsClient.GetQueueAttributesAsync(
            deadLetterQueueUrl,
            [QueueAttributeName.QueueArn],
            cancellationToken)).Attributes[QueueAttributeName.QueueArn];

        var queueUrl = (await sqsClient.CreateQueueAsync(queueName, cancellationToken)).QueueUrl;

        await sqsClient.SetQueueAttributesAsync(
            queueUrl,
            new Dictionary<string, string>
            {
                [QueueAttributeName.RedrivePolicy] = JsonSerializer.Serialize(new
                {
                    deadLetterTargetArn = deadLetterQueueArn,
                    maxReceiveCount = MaxReceiveCount.ToString(),
                }),
            },
            cancellationToken);

        return queueUrl;
    }
}
