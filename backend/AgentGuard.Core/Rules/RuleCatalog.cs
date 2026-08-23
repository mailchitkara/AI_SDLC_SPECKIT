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

    // 006-security-risk-rules: first Phase 2 addition, appended after the original five to
    // preserve their relative order (data-model.md).
    public static readonly Rule OverlyPermissiveAccess =
        new(new RuleId("OVERLY_PERMISSIVE_ACCESS_CONTROL"), "Overly Permissive Access Control", Severity.High, RiskDimension.Security);

    /// <summary>The original five fixed V1 rules, in the fixed order used for CheckResult output (FR-011 from 001-pr-risk-analysis-v1), plus later phases' additions appended after them.</summary>
    public static readonly IReadOnlyList<Rule> All =
    [
        LargeChangeSize,
        MissingRelatedTests,
        ApiContractBreakingChange,
        ArchitectureViolation,
        SecretDetected,
        OverlyPermissiveAccess,
    ];
}
