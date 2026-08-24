namespace AgentGuard.Core.RiskEngine;

/// <summary>What kind of risk a finding represents, independent of severity (FR-002).</summary>
public enum RiskDimension
{
    Security,
    Testing,
    Compatibility,
    Architecture,
    ChangeManagement,
    Dependencies,
    Reliability,
    Configuration,

    // 013-business-critical-path-detection: first Phase 4 dimension addition, appended after the
    // original eight (research.md §2). None of the original eight represent "this code area
    // matters more to the business" — this is orthogonal to Architecture (structural correctness)
    // and ChangeManagement (the nature of how a change was made).
    BusinessCriticality,
}
