using AgentGuard.Core.Findings;
using AgentGuard.Core.PolicyEngine;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-001 through FR-004: flags every changed file matching a configured business-critical path
/// pattern. Unlike 006-011, this is not count-based diffing — every matching file in the PR fires
/// every time, since the risk signal ("this PR's blast radius includes a critical area") applies
/// to the PR as a whole, not to a newly-introduced occurrence (data-model.md).
/// </summary>
public static class BusinessCriticalPathRule
{
    public static IReadOnlyList<Finding> Evaluate(PullRequestChangeSet changeSet, BusinessCriticalPathConfig config)
    {
        if (config.Paths.Count == 0)
        {
            return [];
        }

        var findings = new List<Finding>();

        foreach (var file in changeSet.ChangedFiles)
        {
            foreach (var matched in config.MatchingPaths(file.Path))
            {
                findings.Add(new Finding(
                    RuleId: RuleCatalog.BusinessCriticalPath.Id,
                    RuleName: RuleCatalog.BusinessCriticalPath.Name,
                    Severity: RuleCatalog.BusinessCriticalPath.DefaultSeverity,
                    Explanation: $"This change touches '{file.Path}', which matches the configured business-critical area '{matched.Label}'.",
                    Evidence: $"{matched.Label}: {matched.PathPattern}",
                    Location: file.Path,
                    Remediation: $"This change touches a business-critical area ({matched.Label}) — give it additional review scrutiny.",
                    Dimension: RuleCatalog.BusinessCriticalPath.DefaultDimension,
                    Confidence: Confidence.Certain,
                    Kind: FindingKind.Deterministic));
            }
        }

        return findings;
    }
}
