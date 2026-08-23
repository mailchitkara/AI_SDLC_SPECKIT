using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-004: fires when at least one recognized source/business-logic file changes and no
/// recognized test file changes in the same PR. Intentionally simple file classification only.
/// </summary>
public static class MissingRelatedTestsRule
{
    public static IReadOnlyList<Finding> Evaluate(PullRequestChangeSet changeSet)
    {
        var sourceFiles = changeSet.ChangedFiles
            .Where(f => FileClassifier.Classify(f.Path) == FileClassification.Source)
            .ToList();
        var hasTestChange = changeSet.ChangedFiles
            .Any(f => FileClassifier.Classify(f.Path) == FileClassification.Test);

        if (sourceFiles.Count == 0 || hasTestChange)
        {
            return [];
        }

        return
        [
            new Finding(
                RuleId: RuleCatalog.MissingRelatedTests.Id,
                RuleName: RuleCatalog.MissingRelatedTests.Name,
                Severity: RuleCatalog.MissingRelatedTests.DefaultSeverity,
                Explanation:
                    $"{sourceFiles.Count} source/business-logic file(s) changed but no recognized test file changed in this PR.",
                Evidence: $"Changed source files: {string.Join(", ", sourceFiles.Select(f => f.Path))}",
                Location: null,
                Remediation: "Add or update tests covering the changed source files.",
                Dimension: RuleCatalog.MissingRelatedTests.DefaultDimension,
                Confidence: Confidence.Certain,
                Kind: FindingKind.Deterministic),
        ];
    }
}
