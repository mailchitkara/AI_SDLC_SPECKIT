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
}
