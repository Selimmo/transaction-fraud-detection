namespace TransactionFraudDetection.Worker;

public interface IFraudExplainer
{
    Task<string> ExplainAsync(string prompt);
}
