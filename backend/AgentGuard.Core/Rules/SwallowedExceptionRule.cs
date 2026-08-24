using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-001, FR-002: flags a recognized swallowed-error pattern whose occurrence count increased
/// between a changed file's old and new content. Architecturally identical to
/// DisabledTestRule/OverlyPermissiveAccessRule — differs only in its pattern set and rule identity.
/// </summary>
public static class SwallowedExceptionRule
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

            foreach (var pattern in SwallowedExceptionPatterns.All)
            {
                var oldCount = file.OldContent is null ? 0 : pattern.Pattern.Matches(file.OldContent).Count;
                var newCount = pattern.Pattern.Matches(file.NewContent).Count;
                var newlyIntroduced = newCount - oldCount;

                if (newlyIntroduced <= 0)
                {
                    continue;
                }

                findings.Add(new Finding(
                    RuleId: RuleCatalog.SwallowedException.Id,
                    RuleName: RuleCatalog.SwallowedException.Name,
                    Severity: RuleCatalog.SwallowedException.DefaultSeverity,
                    Explanation: $"Newly introduced content matches a recognized swallowed-error pattern: {pattern.Name}.",
                    Evidence: $"{pattern.Name}: {newlyIntroduced} new occurrence(s)",
                    Location: file.Path,
                    Remediation: pattern.RemediationHint,
                    Dimension: RuleCatalog.SwallowedException.DefaultDimension,
                    Confidence: Confidence.Certain,
                    Kind: FindingKind.Deterministic));
            }
        }

        return findings;
    }
}
