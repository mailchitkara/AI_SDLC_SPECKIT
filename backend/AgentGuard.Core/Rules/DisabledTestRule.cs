using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-001, FR-002: flags a recognized test-skip/ignore pattern whose occurrence count increased
/// between a changed file's old and new content. Count-based, not value-based like
/// SecretDetectedRule — these patterns' matched text is fixed syntax, not a unique value per
/// instance, so a genuinely new second occurrence of an already-present pattern must still be
/// counted as newly introduced (research.md §2). Architecturally identical to
/// OverlyPermissiveAccessRule — differs only in its pattern set and rule identity.
/// </summary>
public static class DisabledTestRule
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

            foreach (var pattern in DisabledTestPatterns.All)
            {
                var oldCount = file.OldContent is null ? 0 : pattern.Pattern.Matches(file.OldContent).Count;
                var newCount = pattern.Pattern.Matches(file.NewContent).Count;
                var newlyIntroduced = newCount - oldCount;

                if (newlyIntroduced <= 0)
                {
                    continue;
                }

                findings.Add(new Finding(
                    RuleId: RuleCatalog.DisabledTest.Id,
                    RuleName: RuleCatalog.DisabledTest.Name,
                    Severity: RuleCatalog.DisabledTest.DefaultSeverity,
                    Explanation: $"Newly introduced content matches a recognized test-skip pattern: {pattern.Name}.",
                    Evidence: $"{pattern.Name}: {newlyIntroduced} new occurrence(s)",
                    Location: file.Path,
                    Remediation: pattern.RemediationHint,
                    Dimension: RuleCatalog.DisabledTest.DefaultDimension,
                    Confidence: Confidence.Certain,
                    Kind: FindingKind.Deterministic));
            }
        }

        return findings;
    }
}
