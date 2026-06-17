using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Domain.Models;
using TransactionFraudDetection.Worker.Tests.Support;

namespace TransactionFraudDetection.Worker.Tests;

public class FraudExplanationProcessorTests
{
    private static FraudCheckNotification Notification(bool isFraudulent, int riskScore = 80, IReadOnlyList<string>? reasons = null) =>
        new(
            Transaction: new Transaction(
                Id: $"txn-{Guid.NewGuid()}",
                AccountId: "acct-1",
                Amount: 7500.00m,
                Currency: "USD",
                Timestamp: DateTimeOffset.Parse("2024-01-01T12:00:00Z"),
                MerchantCountry: "RU",
                AccountHomeCountry: "US"),
            Result: new FraudCheckResult(isFraudulent, riskScore, reasons ?? ["high amount"]));

    private static string CreateTempDir() =>
        Directory.CreateTempSubdirectory("fraud-explanation-tests-").FullName;

    [Fact]
    public async Task Writes_explanation_file_when_fraudulent()
    {
        var outputDir = CreateTempDir();
        var explainer = new FakeFraudExplainer { Response = "It looks fraudulent because..." };
        var processor = new FraudExplanationProcessor(explainer, new ExplanationFileWriter(outputDir));
        var notification = Notification(isFraudulent: true);

        await processor.ProcessAsync(notification);

        var path = Path.Combine(outputDir, $"{notification.Transaction.Id}.json");
        Assert.True(File.Exists(path));
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("It looks fraudulent because...", content);
    }

    [Fact]
    public async Task Does_not_call_explainer_or_write_file_when_not_fraudulent()
    {
        var outputDir = CreateTempDir();
        var explainer = new FakeFraudExplainer();
        var processor = new FraudExplanationProcessor(explainer, new ExplanationFileWriter(outputDir));
        var notification = Notification(isFraudulent: false, riskScore: 0, reasons: []);

        await processor.ProcessAsync(notification);

        Assert.Empty(explainer.ReceivedPrompts);
        Assert.False(Directory.Exists(outputDir) && Directory.EnumerateFiles(outputDir).Any());
    }

    [Fact]
    public async Task Calls_explainer_with_prompt_built_from_notification()
    {
        var outputDir = CreateTempDir();
        var explainer = new FakeFraudExplainer();
        var processor = new FraudExplanationProcessor(explainer, new ExplanationFileWriter(outputDir));
        var notification = Notification(isFraudulent: true);

        await processor.ProcessAsync(notification);

        Assert.Equal([FraudExplanationPromptBuilder.Build(notification)], explainer.ReceivedPrompts);
    }
}
