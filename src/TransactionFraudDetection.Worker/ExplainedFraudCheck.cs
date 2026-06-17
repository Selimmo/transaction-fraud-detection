using TransactionFraudDetection.Domain;

namespace TransactionFraudDetection.Worker;

public record ExplainedFraudCheck(FraudCheckNotification Notification, string Explanation);
