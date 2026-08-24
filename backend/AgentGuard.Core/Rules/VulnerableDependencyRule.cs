using AgentGuard.Core.Dependencies;
using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-001 through FR-005: translates already-identified vulnerable dependencies (supplied by the
/// caller, never resolved by AgentGuard itself — FR-008) into findings. Unlike every other Phase 2
/// rule, this does not scan PullRequestChangeSet content at all (research.md §1).
/// </summary>
public static class VulnerableDependencyRule
{
    public static IReadOnlyList<Finding> Evaluate(IReadOnlyList<VulnerableDependency> vulnerableDependencies) =>
        vulnerableDependencies.Select(BuildFinding).ToList();

    private static Finding BuildFinding(VulnerableDependency dependency)
    {
        var evidence = $"{dependency.PackageName}@{dependency.Version}";
        if (dependency.AdvisoryId is not null)
        {
            evidence += $": {dependency.AdvisoryId}";
        }
        else if (dependency.AdvisoryUrl is not null)
        {
            evidence += $": {dependency.AdvisoryUrl}";
        }

        return new Finding(
            RuleId: RuleCatalog.VulnerableDependency.Id,
            RuleName: RuleCatalog.VulnerableDependency.Name,
            Severity: MapSeverity(dependency.Severity),
            Explanation: $"{dependency.PackageName}@{dependency.Version} has a known vulnerability reported by an external dependency scanner.",
            Evidence: evidence,
            Location: null,
            Remediation: "Upgrade the affected package to a patched version, per the linked advisory.",
            Dimension: RuleCatalog.VulnerableDependency.DefaultDimension,
            Confidence: Confidence.Certain,
            Kind: FindingKind.Deterministic);
    }

    // Critical caps at High, never Blocker — Blocker is reserved exclusively for SECRET_DETECTED's
    // "a credential is now live" certainty (006-security-risk-rules; research.md §3).
    private static Severity MapSeverity(ExternalSeverity severity) => severity switch
    {
        ExternalSeverity.Low => Severity.Low,
        ExternalSeverity.Moderate => Severity.Medium,
        ExternalSeverity.High => Severity.High,
        ExternalSeverity.Critical => Severity.High,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown external severity."),
    };
}
