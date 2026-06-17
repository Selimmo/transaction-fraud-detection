using System.Text.Json;
using TransactionFraudDetection.Domain;

namespace TransactionFraudDetection.Worker;

public class ExplanationFileWriter(string outputDirectory)
{
    public async Task WriteAsync(FraudCheckNotification notification, string explanation)
    {
        Directory.CreateDirectory(outputDirectory);

        var path = Path.Combine(outputDirectory, $"{notification.Transaction.Id}.json");
        var record = new ExplainedFraudCheck(notification, explanation);

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(record));
    }
}
