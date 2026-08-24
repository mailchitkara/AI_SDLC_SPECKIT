using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-001, FR-002: flags a recognized TODO/stub pattern whose occurrence count increased between
/// a changed file's old and new content. Architecturally identical to
/// DisabledTestRule/SwallowedExceptionRule — differs only in its pattern set and rule identity.
/// </summary>
public static class TodoStubRule
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

            foreach (var pattern in TodoStubPatterns.All)
            {
                var oldCount = file.OldContent is null ? 0 : pattern.Pattern.Matches(file.OldContent).Count;
                var newCount = pattern.Pattern.Matches(file.NewContent).Count;
                var newlyIntroduced = newCount - oldCount;

                if (newlyIntroduced <= 0)
                {
                    continue;
                }

                findings.Add(new Finding(
                    RuleId: RuleCatalog.TodoStub.Id,
                    RuleName: RuleCatalog.TodoStub.Name,
                    Severity: RuleCatalog.TodoStub.DefaultSeverity,
                    Explanation: $"Newly introduced content matches a recognized incompleteness pattern: {pattern.Name}.",
                    Evidence: $"{pattern.Name}: {newlyIntroduced} new occurrence(s)",
                    Location: file.Path,
                    Remediation: pattern.RemediationHint,
                    Dimension: RuleCatalog.TodoStub.DefaultDimension,
                    Confidence: Confidence.Certain,
                    Kind: FindingKind.Deterministic));
            }
        }

        return findings;
    }
}
