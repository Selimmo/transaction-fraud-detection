namespace TransactionFraudDetection.Worker;

public record OllamaOptions(string BaseUrl, string Model, int TimeoutSeconds = 120);
