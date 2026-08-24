using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-001 through FR-004: flags a Modified file whose content actually changed and whose path or
/// content matches a recognized generated-file signal. Structurally distinct from the other
/// Phase 2 rules — this checks "did a recognized generated file's content change at all," not
/// "did a specific pattern's occurrence count increase" (research.md §2).
/// </summary>
public static class GeneratedFileModifiedRule
{
    public static IReadOnlyList<Finding> Evaluate(PullRequestChangeSet changeSet)
    {
        var findings = new List<Finding>();

        foreach (var file in changeSet.ChangedFiles)
        {
            if (file.ChangeType != ChangeType.Modified)
            {
                continue;
            }

            if (file.OldContent is null || file.NewContent is null)
            {
                continue;
            }

            if (file.OldContent == file.NewContent)
            {
                continue;
            }

            if (GeneratedFileSignals.ExtensionPattern.IsMatch(file.Path))
            {
                findings.Add(BuildFinding(file.Path, "Recognized Generated-File Extension"));
            }

            if (GeneratedFileSignals.MarkerPattern.IsMatch(file.NewContent))
            {
                findings.Add(BuildFinding(file.Path, "Auto-Generated File Marker"));
            }
        }

        return findings;
    }

    private static Finding BuildFinding(string path, string signalName) =>
        new(
            RuleId: RuleCatalog.GeneratedFileModified.Id,
            RuleName: RuleCatalog.GeneratedFileModified.Name,
            Severity: RuleCatalog.GeneratedFileModified.DefaultSeverity,
            Explanation: $"This file's content changed and it was recognized as generated: {signalName}.",
            Evidence: signalName,
            Location: path,
            Remediation: "Change the source template, schema, or generator instead of editing this generated file directly — hand edits are silently lost the next time it regenerates.",
            Dimension: RuleCatalog.GeneratedFileModified.DefaultDimension,
            Confidence: Confidence.Certain,
            Kind: FindingKind.Deterministic);
}
