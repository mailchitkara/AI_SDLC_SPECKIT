using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-001, FR-002: flags a recognized overly-permissive access-control pattern whose occurrence
/// count increased between a changed file's old and new content. Count-based, not value-based
/// like SecretDetectedRule — these patterns' matched text is fixed syntax, not a unique value per
/// instance, so a genuinely new second occurrence of an already-present pattern must still be
/// counted as newly introduced (research.md §2).
/// </summary>
public static class OverlyPermissiveAccessRule
{
    public static IReadOnlyList<Finding> Evaluate(PullRequestChangeSet changeSet)
    {
        var findings = new List<Finding>();

        foreach (var file in changeSet.ChangedFiles)
        {
            if (file.NewContent is null)
            {
                continue;
            }

            foreach (var pattern in PermissivePatterns.All)
            {
                var oldCount = file.OldContent is null ? 0 : pattern.Pattern.Matches(file.OldContent).Count;
                var newCount = pattern.Pattern.Matches(file.NewContent).Count;
                var newlyIntroduced = newCount - oldCount;

                if (newlyIntroduced <= 0)
                {
                    continue;
                }

                findings.Add(new Finding(
                    RuleId: RuleCatalog.OverlyPermissiveAccess.Id,
                    RuleName: RuleCatalog.OverlyPermissiveAccess.Name,
                    Severity: RuleCatalog.OverlyPermissiveAccess.DefaultSeverity,
                    Explanation: $"Newly introduced content matches a recognized overly-permissive access pattern: {pattern.Name}.",
                    Evidence: $"{pattern.Name}: {newlyIntroduced} new occurrence(s)",
                    Location: file.Path,
                    Remediation: pattern.RemediationHint,
                    Dimension: RuleCatalog.OverlyPermissiveAccess.DefaultDimension,
                    Confidence: Confidence.Certain,
                    Kind: FindingKind.Deterministic));
            }
        }

        return findings;
    }
}
