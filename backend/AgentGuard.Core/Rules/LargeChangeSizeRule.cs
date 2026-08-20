using AgentGuard.Core.Findings;

namespace AgentGuard.Core.Rules;

/// <summary>FR-003: fires when a PR's total changed lines exceed 500 or changed files exceed 20.</summary>
public static class LargeChangeSizeRule
{
    private const int MaxLines = 500;
    private const int MaxFiles = 20;

    public static IReadOnlyList<Finding> Evaluate(PullRequestChangeSet changeSet)
    {
        var totalLines = changeSet.ChangedFiles.Sum(f => f.LinesAdded + f.LinesDeleted);
        var fileCount = changeSet.ChangedFiles.Count;

        if (totalLines <= MaxLines && fileCount <= MaxFiles)
        {
            return [];
        }

        return
        [
            new Finding(
                RuleId: RuleCatalog.LargeChangeSize.Id,
                RuleName: RuleCatalog.LargeChangeSize.Name,
                Severity: RuleCatalog.LargeChangeSize.DefaultSeverity,
                Explanation:
                    $"This PR changes {totalLines} lines across {fileCount} files, exceeding the large-change " +
                    $"thresholds of {MaxLines} lines or {MaxFiles} files.",
                Evidence: $"{totalLines} lines changed across {fileCount} files",
                Location: null,
                Remediation: "Consider splitting this PR into smaller, focused changes."),
        ];
    }
}
