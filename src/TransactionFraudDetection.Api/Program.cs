using Amazon.SQS;
using TransactionFraudDetection.Api;
using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Domain.Rules;
using TransactionFraudDetection.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var fraudRuleOptions = builder.Configuration.GetSection("FraudRules").Get<FraudRulesOptions>() ?? new FraudRulesOptions();

builder.Services.AddSingleton<IFraudRule>(new HighAmountRule(fraudRuleOptions.HighAmountThreshold, fraudRuleOptions.HighAmountScore));
builder.Services.AddSingleton<IFraudRule>(new VelocityRule(
    TimeSpan.FromMinutes(fraudRuleOptions.VelocityWindowMinutes), fraudRuleOptions.VelocityMaxTransactionsInWindow, fraudRuleOptions.VelocityScore));
builder.Services.AddSingleton<IFraudRule>(new GeoMismatchRule(fraudRuleOptions.GeoMismatchScore));
builder.Services.AddSingleton<IFraudRule>(new OddHoursRule(
    TimeSpan.FromHours(fraudRuleOptions.OddHoursWindowStartHour), TimeSpan.FromHours(fraudRuleOptions.OddHoursWindowEndHour), fraudRuleOptions.OddHoursScore));
builder.Services.AddSingleton(sp => new FraudDetectionEngine(sp.GetServices<IFraudRule>(), fraudRuleOptions.RiskThreshold));

var sqsOptions = builder.Configuration.GetSection("Sqs").Get<SqsOptions>()
    ?? throw new InvalidOperationException("Missing 'Sqs' configuration section.");

builder.Services.AddSingleton<IAppEnvironment>(new AppEnvironment(builder.Environment.EnvironmentName));
builder.Services.AddSingleton<IAmazonSQS>(sp =>
    SqsClientFactory.Create(sqsOptions, sp.GetRequiredService<IAppEnvironment>()));
builder.Services.AddSingleton(sp =>
    new SqsQueueResolver(sp.GetRequiredService<IAmazonSQS>(), sqsOptions.QueueName, sp.GetRequiredService<IAppEnvironment>()));
builder.Services.AddSingleton<SqsFindingsPublisher>();

var failedPublishDirectory = builder.Configuration["FailedPublishDirectory"] ?? "failed-publishes";
builder.Services.AddSingleton(new FailedPublishArchiver(failedPublishDirectory));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/fraud-check", async (
    FraudCheckContext context,
    FraudDetectionEngine engine,
    SqsFindingsPublisher publisher,
    FailedPublishArchiver archiver,
    ILogger<Program> logger) =>
{
    var result = engine.Evaluate(context);
    var notification = new FraudCheckNotification(context.Transaction, result);

    try
    {
        await publisher.PublishAsync(notification);
    }
    catch (Exception ex)
    {
        // The fraud verdict is already decided; a queue outage shouldn't fail the
        // authorization the payment system is blocked on. Archive the notification
        // so it can be replayed/batch-published later instead of losing it.
        logger.LogError(ex, "Failed to publish fraud check finding for transaction {TransactionId}", context.Transaction.Id);
        await archiver.ArchiveAsync(notification);
    }

    return result;
})
    .WithName("CheckTransactionForFraud");

app.Run();
