namespace TransactionFraudDetection.Domain.Models;

public record Transaction(
    string Id,
    string AccountId,
    decimal Amount,
    string Currency,
    DateTimeOffset Timestamp,
    string MerchantCountry,
    string AccountHomeCountry);
