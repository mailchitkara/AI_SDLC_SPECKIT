namespace AgentGuard.Core.RiskEngine;

public enum Severity
{
    Info,
    Low,
    Medium,
    High,
    Blocker,
}

public static class SeverityWeights
{
    public static int WeightOf(Severity severity) => severity switch
    {
        Severity.Info => 0,
        Severity.Low => 10,
        Severity.Medium => 20,
        Severity.High => 35,
        Severity.Blocker => 100,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity."),
    };
}
