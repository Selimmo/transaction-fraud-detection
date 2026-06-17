using TransactionFraudDetection.Domain.Models;

namespace TransactionFraudDetection.Domain;

public record FraudCheckContext(Transaction Transaction, IReadOnlyList<Transaction> RecentTransactions);
