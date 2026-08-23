using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-007 + FR-010: flags newly-introduced content matching a recognized secret pattern.
/// Findings are constructed only from the already-masked value — the raw match is never
/// held past this point, so it can never leak into a Finding, response, UI, or log.
///
/// MandatoryOverride is intentionally left false here — this rule already reaches BLOCK_MERGE
/// via its BLOCKER severity weight alone, so setting it too would be redundant and would muddy
/// what MandatoryOverride actually signals to a consumer (005-risk-engine-foundation research.md §3).
/// </summary>
public static class SecretDetectedRule
{
    public static IReadOnlyList<Finding> Evaluate(PullRequestChangeSet changeSet)
    {
        var findings = new List<Finding>();

        foreach (var file in changeSet.ChangedFiles)
        {
            foreach (var rawSecret in NewlyIntroducedSecrets(file))
            {
                findings.Add(new Finding(
                    RuleId: RuleCatalog.SecretDetected.Id,
                    RuleName: RuleCatalog.SecretDetected.Name,
                    Severity: RuleCatalog.SecretDetected.DefaultSeverity,
                    Explanation: "Changed content matches a recognized secret pattern.",
                    Evidence: EvidenceMasking.Mask(rawSecret),
                    Location: file.Path,
                    Remediation: "Remove the secret from source control and rotate the credential immediately.",
                    Dimension: RuleCatalog.SecretDetected.DefaultDimension,
                    Confidence: Confidence.Certain,
                    Kind: FindingKind.Deterministic));
            }
        }

        return findings;
    }

    private static IEnumerable<string> NewlyIntroducedSecrets(ChangedFile file)
    {
        if (file.NewContent is null)
        {
            yield break;
        }

        var oldSecrets = file.OldContent is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(ExtractSecrets(file.OldContent), StringComparer.Ordinal);

        foreach (var secret in ExtractSecrets(file.NewContent))
        {
            if (!oldSecrets.Contains(secret))
            {
                yield return secret;
            }
        }
    }

    private static IEnumerable<string> ExtractSecrets(string content)
    {
        foreach (var secretPattern in SecretPatterns.All)
        {
            foreach (System.Text.RegularExpressions.Match match in secretPattern.Pattern.Matches(content))
            {
                yield return match.Groups.Count > 1 ? match.Groups[^1].Value : match.Value;
            }
        }
    }
}
