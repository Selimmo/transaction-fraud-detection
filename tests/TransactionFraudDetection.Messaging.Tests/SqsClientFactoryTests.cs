using Amazon.SQS;

namespace TransactionFraudDetection.Messaging.Tests;

public class SqsClientFactoryTests
{
    private static readonly SqsOptions LocalOptions = new(
        ServiceUrl: "http://localhost:4566",
        Region: "us-east-1",
        AccessKey: "test",
        SecretKey: "test",
        QueueName: "fraud-check-findings");

    [Fact]
    public void Development_points_the_client_at_the_configured_service_url()
    {
        var client = (AmazonSQSClient)SqsClientFactory.Create(LocalOptions, new AppEnvironment("Development"));

        Assert.Equal(LocalOptions.ServiceUrl + "/", client.Config.ServiceURL);
    }

    [Fact]
    public void Production_uses_the_default_credential_chain_instead_of_the_configured_service_url()
    {
        var options = LocalOptions with { ServiceUrl = null, AccessKey = null, SecretKey = null };

        var client = (AmazonSQSClient)SqsClientFactory.Create(options, new AppEnvironment("Production"));

        Assert.Null(client.Config.ServiceURL);
        Assert.Equal(LocalOptions.Region, client.Config.RegionEndpoint?.SystemName);
    }
}
