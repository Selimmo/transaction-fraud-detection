namespace TransactionFraudDetection.Messaging;

public record SqsOptions(string? ServiceUrl, string Region, string? AccessKey, string? SecretKey, string QueueName);
