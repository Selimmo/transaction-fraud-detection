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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/fraud-check", (FraudCheckContext context, FraudDetectionEngine engine) =>
    engine.Evaluate(context))
    .WithName("CheckTransactionForFraud");

app.Run();
