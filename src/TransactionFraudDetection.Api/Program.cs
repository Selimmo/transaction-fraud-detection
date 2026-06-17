using Amazon.Runtime;
using Amazon.SQS;
using TransactionFraudDetection.Api;
using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Domain.Rules;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IFraudRule, HighAmountRule>();
builder.Services.AddSingleton<IFraudRule, VelocityRule>();
builder.Services.AddSingleton<IFraudRule, GeoMismatchRule>();
builder.Services.AddSingleton<IFraudRule, OddHoursRule>();
builder.Services.AddSingleton<FraudDetectionEngine>();

builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(
    new BasicAWSCredentials("test", "test"),
    new AmazonSQSConfig { ServiceURL = "http://localhost:4566", AuthenticationRegion = "us-east-1" }));
builder.Services.AddSingleton(sp =>
    new SqsFindingsPublisher(sp.GetRequiredService<IAmazonSQS>(), queueName: "fraud-check-findings"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/fraud-check", async (FraudCheckContext context, FraudDetectionEngine engine, SqsFindingsPublisher publisher) =>
{
    var result = engine.Evaluate(context);
    await publisher.PublishAsync(new FraudCheckNotification(context.Transaction, result));
    return result;
})
    .WithName("CheckTransactionForFraud");

app.Run();
