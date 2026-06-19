using Microsoft.Extensions.Configuration;
using TransactionFraudDetection.Messaging;
using TransactionFraudDetection.Worker;

var appEnvironment = new AppEnvironment(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production");

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{appEnvironment.Name}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var sqsOptions = configuration.GetSection("Sqs").Get<SqsOptions>()
    ?? throw new InvalidOperationException("Missing 'Sqs' configuration section.");
var ollamaOptions = configuration.GetSection("Ollama").Get<OllamaOptions>()
    ?? throw new InvalidOperationException("Missing 'Ollama' configuration section.");
var outputDirectory = configuration["OutputDirectory"] ?? "output";

var sqsClient = SqsClientFactory.Create(sqsOptions, appEnvironment);
var queueResolver = new SqsQueueResolver(sqsClient, sqsOptions.QueueName, appEnvironment);

var httpClient = new HttpClient
{
    BaseAddress = new Uri(ollamaOptions.BaseUrl),
    Timeout = TimeSpan.FromSeconds(ollamaOptions.TimeoutSeconds),
};
var explainer = new OllamaFraudExplainer(httpClient, ollamaOptions.Model);
var fileWriter = new ExplanationFileWriter(Path.Combine(Directory.GetCurrentDirectory(), outputDirectory));
var processor = new FraudExplanationProcessor(explainer, fileWriter);
var listener = new FindingsQueueListener(sqsClient, queueResolver, processor);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Worker listening for fraud check findings. Press Ctrl+C to stop.");
await listener.RunAsync(cts.Token);
