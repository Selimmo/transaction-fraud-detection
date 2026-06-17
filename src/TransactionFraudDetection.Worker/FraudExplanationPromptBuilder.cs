using TransactionFraudDetection.Domain;

namespace TransactionFraudDetection.Worker;

public static class FraudExplanationPromptBuilder
{
    public static string Build(FraudCheckNotification notification)
    {
        var transaction = notification.Transaction;
        var result = notification.Result;
        var reasons = string.Join(", ", result.TriggeredReasons);

        return $"""
            A rule-based fraud detection system flagged this transaction as fraudulent.
            Amount: {transaction.Amount} {transaction.Currency}
            Merchant country: {transaction.MerchantCountry}
            Account home country: {transaction.AccountHomeCountry}
            Risk score: {result.RiskScore}
            Triggered rules: {reasons}

            In 2-3 plain-language sentences, explain why this transaction looks fraudulent.
            """;
    }
}
