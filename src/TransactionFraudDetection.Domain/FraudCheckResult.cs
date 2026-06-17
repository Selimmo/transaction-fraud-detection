namespace TransactionFraudDetection.Domain;

public record FraudCheckResult(bool IsFraudulent, int RiskScore, IReadOnlyList<string> TriggeredReasons);
