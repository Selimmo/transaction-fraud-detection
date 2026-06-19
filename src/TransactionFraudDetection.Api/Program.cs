using Amazon.SQS;
using TransactionFraudDetection.Api;
using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Domain.Rules;
using TransactionFraudDetection.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IFraudRule, HighAmountRule>();
builder.Services.AddSingleton<IFraudRule, VelocityRule>();
builder.Services.AddSingleton<IFraudRule, GeoMismatchRule>();
builder.Services.AddSingleton<IFraudRule, OddHoursRule>();
builder.Services.AddSingleton<FraudDetectionEngine>();

var sqsOptions = builder.Configuration.GetSection("Sqs").Get<SqsOptions>()
    ?? throw new InvalidOperationException("Missing 'Sqs' configuration section.");

builder.Services.AddSingleton<IAppEnvironment>(new AppEnvironment(builder.Environment.EnvironmentName));
builder.Services.AddSingleton<IAmazonSQS>(sp =>
    SqsClientFactory.Create(sqsOptions, sp.GetRequiredService<IAppEnvironment>()));
builder.Services.AddSingleton(sp =>
    new SqsQueueResolver(sp.GetRequiredService<IAmazonSQS>(), sqsOptions.QueueName));
builder.Services.AddSingleton<SqsFindingsPublisher>();

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
