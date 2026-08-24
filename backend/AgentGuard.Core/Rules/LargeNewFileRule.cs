using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-001, FR-002: flags a newly-added file whose line count meets or exceeds a fixed threshold —
/// brand-new code with no prior review or production history (research.md §1). "New in this PR"
/// is a deliberately narrower proxy for the phase's true novelty signal (a file's actual age
/// across the repository's full history), which needs a new GitHub commit-history integration
/// out of scope for this increment.
/// </summary>
public static class LargeNewFileRule
{
    private const int LineThreshold = 200;

    public static IReadOnlyList<Finding> Evaluate(PullRequestChangeSet changeSet)
    {
        var findings = new List<Finding>();

        foreach (var file in changeSet.ChangedFiles)
        {
            if (file.ChangeType != ChangeType.Added || file.LinesAdded < LineThreshold)
            {
                continue;
            }

            findings.Add(new Finding(
                RuleId: RuleCatalog.LargeNewFile.Id,
                RuleName: RuleCatalog.LargeNewFile.Name,
                Severity: RuleCatalog.LargeNewFile.DefaultSeverity,
                Explanation: $"'{file.Path}' is a brand-new file introducing {file.LinesAdded} lines, with no prior review or production history.",
                Evidence: $"{file.LinesAdded} lines in a newly-added file",
                Location: file.Path,
                Remediation: "This file has no prior review or production history — consider extra scrutiny, or splitting it if it bundles multiple unrelated concerns.",
                Dimension: RuleCatalog.LargeNewFile.DefaultDimension,
                Confidence: Confidence.Certain,
                Kind: FindingKind.Deterministic));
        }

        return findings;
    }
}
