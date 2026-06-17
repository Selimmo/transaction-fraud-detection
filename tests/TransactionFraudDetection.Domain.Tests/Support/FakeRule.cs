using TransactionFraudDetection.Domain.Rules;

namespace TransactionFraudDetection.Domain.Tests.Support;

internal class FakeRule(string name, FraudRuleResult result) : IFraudRule
{
    public string Name => name;

    public FraudRuleResult Evaluate(FraudCheckContext context) => result;
}
