namespace AgentGuard.Core.RiskEngine;

/// <summary>How certain a finding is (FR-005). Deterministic findings are always Certain.</summary>
public enum Confidence
{
    Certain,
    High,
    Medium,
    Low,
}
