using System.Text.Json;
using TransactionFraudDetection.Domain;

namespace TransactionFraudDetection.Api;

public class FailedPublishArchiver(string outputDirectory)
{
    public async Task ArchiveAsync(FraudCheckNotification notification)
    {
        Directory.CreateDirectory(outputDirectory);

        var path = Path.Combine(outputDirectory, $"{notification.Transaction.Id}.json");

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(notification));
    }
}
