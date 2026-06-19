namespace TransactionFraudDetection.Messaging;

public interface IAppEnvironment
{
    string Name { get; }
    bool IsDevelopment { get; }
    bool IsProduction { get; }
}

public class AppEnvironment(string name) : IAppEnvironment
{
    public string Name { get; } = name;
    public bool IsDevelopment => Name.Equals("Development", StringComparison.OrdinalIgnoreCase);
    public bool IsProduction => Name.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
