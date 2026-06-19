namespace TransactionFraudDetection.Domain;

public record FraudRulesOptions(
    decimal HighAmountThreshold = 5000m,
    int HighAmountScore = 50,
    int VelocityWindowMinutes = 5,
    int VelocityMaxTransactionsInWindow = 3,
    int VelocityScore = 40,
    int GeoMismatchScore = 30,
    int OddHoursWindowStartHour = 1,
    int OddHoursWindowEndHour = 5,
    int OddHoursScore = 20,
    int RiskThreshold = 50);
