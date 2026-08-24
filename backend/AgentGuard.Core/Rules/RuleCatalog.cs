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

    // 007-disabled-test-detection: second Phase 2 addition, appended after OverlyPermissiveAccess
    // to preserve the existing six rules' relative order (data-model.md).
    public static readonly Rule DisabledTest =
        new(new RuleId("DISABLED_TEST_INTRODUCED"), "Newly Disabled Test", Severity.High, RiskDimension.Testing);

    // 008-swallowed-exception-detection: third Phase 2 addition, appended after DisabledTest to
    // preserve the existing seven rules' relative order (data-model.md).
    public static readonly Rule SwallowedException =
        new(new RuleId("SWALLOWED_EXCEPTION_INTRODUCED"), "Newly Swallowed Exception", Severity.High, RiskDimension.Reliability);

    // 009-generated-file-contamination: fourth Phase 2 addition, appended after SwallowedException
    // to preserve the existing eight rules' relative order (data-model.md).
    public static readonly Rule GeneratedFileModified =
        new(new RuleId("GENERATED_FILE_MODIFIED"), "Hand-Edited Generated File", Severity.Medium, RiskDimension.ChangeManagement);

    // 010-todo-stub-detection: fifth Phase 2 addition, appended after GeneratedFileModified to
    // preserve the existing nine rules' relative order (data-model.md).
    public static readonly Rule TodoStub =
        new(new RuleId("TODO_STUB_INTRODUCED"), "Newly Introduced TODO or Stub", Severity.Medium, RiskDimension.ChangeManagement);

    // 011-insecure-configuration-detection: sixth Phase 2 addition, appended after TodoStub to
    // preserve the existing ten rules' relative order (data-model.md).
    public static readonly Rule InsecureConfiguration =
        new(new RuleId("INSECURE_CONFIGURATION_INTRODUCED"), "Insecure Configuration", Severity.High, RiskDimension.Configuration);

    // 012-vulnerable-dependency-adapter: seventh and final Phase 2 addition, appended after
    // InsecureConfiguration to preserve the existing eleven rules' relative order (data-model.md).
    // DefaultSeverity is nominal only — actual per-finding severity is computed from each supplied
    // entry's own external severity (research.md §3), not read from this default.
    public static readonly Rule VulnerableDependency =
        new(new RuleId("VULNERABLE_DEPENDENCY_DETECTED"), "Vulnerable Dependency", Severity.High, RiskDimension.Dependencies);

    /// <summary>The original five fixed V1 rules, in the fixed order used for CheckResult output (FR-011 from 001-pr-risk-analysis-v1), plus later phases' additions appended after them.</summary>
    public static readonly IReadOnlyList<Rule> All =
    [
        LargeChangeSize,
        MissingRelatedTests,
        ApiContractBreakingChange,
        ArchitectureViolation,
        SecretDetected,
        OverlyPermissiveAccess,
        DisabledTest,
        SwallowedException,
        GeneratedFileModified,
        TodoStub,
        InsecureConfiguration,
        VulnerableDependency,
    ];
}
