using Amazon;
using Amazon.Runtime;
using Amazon.SQS;

namespace TransactionFraudDetection.Messaging;

public static class SqsClientFactory
{
    public static IAmazonSQS Create(SqsOptions options, IAppEnvironment environment) =>
        environment.IsDevelopment
            ? new AmazonSQSClient(
                new BasicAWSCredentials(options.AccessKey, options.SecretKey),
                new AmazonSQSConfig { ServiceURL = options.ServiceUrl, AuthenticationRegion = options.Region })
            : new AmazonSQSClient(RegionEndpoint.GetBySystemName(options.Region));
}
