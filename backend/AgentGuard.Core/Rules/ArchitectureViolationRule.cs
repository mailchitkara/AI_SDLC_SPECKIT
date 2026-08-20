using System.Text.RegularExpressions;
using AgentGuard.Core.Findings;
using AgentGuard.Core.PolicyEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-006: text-level scan of added import/using statements against a configured set of
/// forbidden dependency relationships — no graph-based static analysis in V1 (research.md §5).
/// </summary>
public static partial class ArchitectureViolationRule
{
    public static IReadOnlyList<Finding> Evaluate(PullRequestChangeSet changeSet, ForbiddenDependencyConfig config)
    {
        if (config.Relationships.Count == 0)
        {
            return [];
        }

        var findings = new List<Finding>();

        foreach (var file in changeSet.ChangedFiles)
        {
            foreach (var import in AddedImports(file))
            {
                foreach (var relationship in config.MatchingRelationships(file.Path, import))
                {
                    findings.Add(new Finding(
                        RuleId: RuleCatalog.ArchitectureViolation.Id,
                        RuleName: RuleCatalog.ArchitectureViolation.Name,
                        Severity: RuleCatalog.ArchitectureViolation.DefaultSeverity,
                        Explanation:
                            $"'{file.Path}' (matching forbidden source pattern '{relationship.From}') now depends on " +
                            $"'{import}', which matches the forbidden target pattern '{relationship.To}'.",
                        Evidence: $"{file.Path} -> {import}",
                        Location: file.Path,
                        Remediation: "Remove this dependency or restructure the code to respect the configured architecture boundary."));
                }
            }
        }

        return findings;
    }

    private static IEnumerable<string> AddedImports(ChangedFile file)
    {
        if (file.NewContent is null)
        {
            return [];
        }

        var newImports = ExtractImports(file.NewContent);

        if (file.ChangeType == ChangeType.Added || file.OldContent is null)
        {
            return newImports;
        }

        var oldImports = new HashSet<string>(ExtractImports(file.OldContent), StringComparer.Ordinal);
        return newImports.Where(i => !oldImports.Contains(i));
    }

    private static IEnumerable<string> ExtractImports(string content)
    {
        foreach (Match match in CSharpUsingPattern().Matches(content))
        {
            yield return match.Groups[1].Value;
        }

        foreach (Match match in EsImportFromPattern().Matches(content))
        {
            yield return match.Groups[1].Value;
        }

        foreach (Match match in EsRequirePattern().Matches(content))
        {
            yield return match.Groups[1].Value;
        }

        foreach (Match match in PythonImportPattern().Matches(content))
        {
            yield return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        }
    }

    [GeneratedRegex(@"^\s*using\s+([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex CSharpUsingPattern();

    [GeneratedRegex(@"\bfrom\s+['""]([^'""]+)['""]")]
    private static partial Regex EsImportFromPattern();

    [GeneratedRegex(@"\brequire\(\s*['""]([^'""]+)['""]\s*\)")]
    private static partial Regex EsRequirePattern();

    [GeneratedRegex(@"^\s*(?:from\s+([\w.]+)\s+import|import\s+([\w.]+))", RegexOptions.Multiline)]
    private static partial Regex PythonImportPattern();
}
