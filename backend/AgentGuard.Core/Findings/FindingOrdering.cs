namespace AgentGuard.Core.Findings;

/// <summary>
/// Produces the fixed, deterministic ordering used for RiskAnalysisResult.Findings:
/// severity descending (BLOCKER first), then rule id, so identical input always
/// yields byte-for-byte identical output (FR-013, research.md §7).
/// </summary>
public static class FindingOrdering
{
    public static IReadOnlyList<Finding> Stable(IEnumerable<Finding> findings) =>
        findings
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.RuleId.Value, StringComparer.Ordinal)
            .ToList();
}
