using Amazon.Runtime;
using Amazon.SQS;
using TransactionFraudDetection.Worker;

var sqsClient = new AmazonSQSClient(
    new BasicAWSCredentials("test", "test"),
    new AmazonSQSConfig { ServiceURL = "http://localhost:4566", AuthenticationRegion = "us-east-1" });

var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };
var explainer = new OllamaFraudExplainer(httpClient);
var fileWriter = new ExplanationFileWriter(Path.Combine(Directory.GetCurrentDirectory(), "output"));
var processor = new FraudExplanationProcessor(explainer, fileWriter);
var listener = new FindingsQueueListener(sqsClient, queueName: "fraud-check-findings", processor);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Worker listening for fraud check findings. Press Ctrl+C to stop.");
await listener.RunAsync(cts.Token);
