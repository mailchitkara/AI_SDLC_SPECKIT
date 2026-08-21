using System.Text.RegularExpressions;

namespace AgentGuard.Core.Rules;

public sealed record SecretPattern(string Name, Regex Pattern);

/// <summary>
/// Fixed set of regex patterns for common secret shapes (FR-007, research.md §6). Deliberately
/// limited to well-known, low-false-positive prefixed formats rather than generic entropy analysis.
/// </summary>
public static partial class SecretPatterns
{
    public static readonly IReadOnlyList<SecretPattern> All =
    [
        new("AWS Access Key ID", AwsAccessKeyPattern()),
        new("Google API Key", GoogleApiKeyPattern()),
        new("GitHub Token", GitHubTokenPattern()),
        new("Slack Token", SlackTokenPattern()),
        new("Private Key", PrivateKeyPattern()),
        new("Generic Secret Assignment", GenericSecretAssignmentPattern()),
    ];

    [GeneratedRegex(@"\bAKIA[0-9A-Z]{16}\b")]
    private static partial Regex AwsAccessKeyPattern();

    [GeneratedRegex(@"\bAIza[0-9A-Za-z\-_]{35}\b")]
    private static partial Regex GoogleApiKeyPattern();

    [GeneratedRegex(@"\bgh[pousr]_[A-Za-z0-9]{36,}\b")]
    private static partial Regex GitHubTokenPattern();

    [GeneratedRegex(@"\bxox[baprs]-[A-Za-z0-9-]{10,}\b")]
    private static partial Regex SlackTokenPattern();

    [GeneratedRegex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----[\s\S]*?-----END [A-Z ]*PRIVATE KEY-----")]
    private static partial Regex PrivateKeyPattern();

    [GeneratedRegex(@"(?i)\b(api[_-]?key|secret|token|password)\b\s*[:=]\s*['""]([^'""\s]{12,})['""]")]
    private static partial Regex GenericSecretAssignmentPattern();
}
