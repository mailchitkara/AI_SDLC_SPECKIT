using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

public sealed record Rule(RuleId Id, string Name, Severity DefaultSeverity, RiskDimension DefaultDimension);

public static class RuleCatalog
{
    public static readonly Rule LargeChangeSize =
        new(new RuleId("LARGE_CHANGE_SIZE"), "Large Change Size", Severity.Low, RiskDimension.ChangeManagement);

    public static readonly Rule MissingRelatedTests =
        new(new RuleId("MISSING_RELATED_TESTS"), "Missing Related Tests", Severity.Medium, RiskDimension.Testing);

    public static readonly Rule ApiContractBreakingChange =
        new(new RuleId("API_CONTRACT_BREAKING_CHANGE"), "API Contract Breaking Change", Severity.High, RiskDimension.Compatibility);

    public static readonly Rule ArchitectureViolation =
        new(new RuleId("ARCHITECTURE_VIOLATION"), "Architecture / Dependency Violation", Severity.High, RiskDimension.Architecture);

    public static readonly Rule SecretDetected =
        new(new RuleId("SECRET_DETECTED"), "Potential Secret Detected", Severity.Blocker, RiskDimension.Security);

    /// <summary>All five fixed V1 rules, in the fixed order used for CheckResult output (FR-011).</summary>
    public static readonly IReadOnlyList<Rule> All =
    [
        LargeChangeSize,
        MissingRelatedTests,
        ApiContractBreakingChange,
        ArchitectureViolation,
        SecretDetected,
    ];
}
