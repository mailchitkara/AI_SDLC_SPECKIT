namespace AgentGuard.Core.RiskEngine;

/// <summary>
/// The score bands used to derive a RiskClassification (FR-007). Supplied per-request only —
/// never persisted server-side (Clarifications, 2026-08-23).
/// </summary>
public sealed record ThresholdConfiguration(int LowMax, int MediumMax, int HighMax)
{
    /// <summary>Reproduces V1's fixed bands exactly: 0-24 Low, 25-49 Medium, 50-74 High, 75-100 Critical.</summary>
    public static readonly ThresholdConfiguration Default = new(24, 49, 74);
}
