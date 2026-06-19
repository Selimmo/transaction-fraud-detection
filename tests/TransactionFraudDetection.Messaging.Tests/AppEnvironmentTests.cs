namespace TransactionFraudDetection.Messaging.Tests;

public class AppEnvironmentTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    [InlineData("DEVELOPMENT")]
    public void IsDevelopment_true_for_development_regardless_of_case(string name)
    {
        var environment = new AppEnvironment(name);

        Assert.True(environment.IsDevelopment);
        Assert.False(environment.IsProduction);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("production")]
    public void IsProduction_true_for_production_regardless_of_case(string name)
    {
        var environment = new AppEnvironment(name);

        Assert.True(environment.IsProduction);
        Assert.False(environment.IsDevelopment);
    }

    [Fact]
    public void Staging_is_neither_development_nor_production()
    {
        var environment = new AppEnvironment("Staging");

        Assert.False(environment.IsDevelopment);
        Assert.False(environment.IsProduction);
    }

    [Fact]
    public void Name_returns_the_value_passed_in()
    {
        var environment = new AppEnvironment("Staging");

        Assert.Equal("Staging", environment.Name);
    }
}
