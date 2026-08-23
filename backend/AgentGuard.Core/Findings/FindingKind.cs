namespace AgentGuard.Core.Findings;

/// <summary>Whether a finding came from an exact, rule-based check or an inference (FR-004).</summary>
public enum FindingKind
{
    Deterministic,
    Contextual,
}
