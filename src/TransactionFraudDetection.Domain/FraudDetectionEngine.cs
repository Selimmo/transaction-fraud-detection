using TransactionFraudDetection.Domain.Rules;

namespace TransactionFraudDetection.Domain;

public class FraudDetectionEngine(IEnumerable<IFraudRule> rules, int riskThreshold = 50)
{
    public FraudCheckResult Evaluate(FraudCheckContext context)
    {
        var triggered = rules
            .Select(rule => rule.Evaluate(context))
            .Where(result => result.Triggered)
            .ToList();

        var riskScore = triggered.Sum(result => result.Score);

        return new FraudCheckResult(
            IsFraudulent: riskScore >= riskThreshold,
            RiskScore: riskScore,
            TriggeredReasons: triggered.Select(result => result.Reason).ToList());
    }
}
