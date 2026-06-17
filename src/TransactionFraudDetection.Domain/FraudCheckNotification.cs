using TransactionFraudDetection.Domain.Models;

namespace TransactionFraudDetection.Domain;

public record FraudCheckNotification(Transaction Transaction, FraudCheckResult Result);
