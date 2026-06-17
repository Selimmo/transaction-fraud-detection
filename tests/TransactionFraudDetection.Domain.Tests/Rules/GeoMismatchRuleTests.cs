using TransactionFraudDetection.Domain;
using TransactionFraudDetection.Domain.Rules;
using TransactionFraudDetection.Domain.Tests.Support;

namespace TransactionFraudDetection.Domain.Tests.Rules;

public class GeoMismatchRuleTests
{
    private readonly GeoMismatchRule _rule = new();

    [Fact]
    public void Does_not_trigger_when_merchant_country_matches_home_country()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(merchantCountry: "US", accountHomeCountry: "US"),
            RecentTransactions: []);

        var result = _rule.Evaluate(context);

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Does_not_trigger_when_countries_match_with_different_casing()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(merchantCountry: "us", accountHomeCountry: "US"),
            RecentTransactions: []);

        var result = _rule.Evaluate(context);

        Assert.False(result.Triggered);
    }

    [Fact]
    public void Triggers_when_merchant_country_differs_from_home_country()
    {
        var context = new FraudCheckContext(
            TransactionFactory.Create(merchantCountry: "RU", accountHomeCountry: "US"),
            RecentTransactions: []);

        var result = _rule.Evaluate(context);

        Assert.True(result.Triggered);
        Assert.Equal(30, result.Score);
    }
}
