namespace TransactionFraudDetection.Worker.Tests.Support;

internal class FakeFraudExplainer : IFraudExplainer
{
    public List<string> ReceivedPrompts { get; } = [];

    public string Response { get; set; } = "explanation";

    public Task<string> ExplainAsync(string prompt)
    {
        ReceivedPrompts.Add(prompt);
        return Task.FromResult(Response);
    }
}
