using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

public sealed record Rule(RuleId Id, string Name, Severity DefaultSeverity);

public static class RuleCatalog
{
    public static readonly Rule LargeChangeSize =
        new(RuleId.LargeChangeSize, "Large Change Size", Severity.Low);

    public static readonly Rule MissingRelatedTests =
        new(RuleId.MissingRelatedTests, "Missing Related Tests", Severity.Medium);

    public static readonly Rule ApiContractBreakingChange =
        new(RuleId.ApiContractBreakingChange, "API Contract Breaking Change", Severity.High);

    public static readonly Rule ArchitectureViolation =
        new(RuleId.ArchitectureViolation, "Architecture / Dependency Violation", Severity.High);

    public static readonly Rule SecretDetected =
        new(RuleId.SecretDetected, "Potential Secret Detected", Severity.Blocker);

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
