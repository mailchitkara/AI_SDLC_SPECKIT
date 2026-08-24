using System.Text.RegularExpressions;

namespace AgentGuard.Core.Rules;

public sealed record InsecureConfigurationPattern(string Name, Regex Pattern, string RemediationHint);

/// <summary>
/// Fixed set of insecure-configuration patterns (011-insecure-configuration-detection FR-001,
/// research.md §1). Deliberately narrow and text-pattern-based, mirroring PermissivePatterns'
/// shape, rather than a general configuration/infrastructure-as-code analyzer (FR-008).
/// </summary>
public static partial class InsecureConfigurationPatterns
{
    public static readonly IReadOnlyList<InsecureConfigurationPattern> All =
    [
        new(
            "Debug Mode Enabled (Django)",
            DjangoDebugModePattern(),
            "Disable debug mode outside local development, or drive it from an environment variable that defaults to off."),
        new(
            "TLS Certificate Validation Disabled (.NET)",
            DotNetTlsDisabledPattern(),
            "Remove the callback that unconditionally accepts every certificate — validate the certificate properly, or scope the bypass to a build configuration that never ships."),
        new(
            "TLS Certificate Validation Disabled (Node.js)",
            NodeTlsDisabledPattern(),
            "Remove the option that disables certificate rejection — validate the certificate properly, or scope the bypass to local development tooling only."),
        new(
            "TLS Certificate Validation Disabled (Python requests)",
            PythonRequestsTlsDisabledPattern(),
            "Remove the argument that disables TLS verification — validate the certificate properly, or scope the bypass to local development tooling only."),
    ];

    [GeneratedRegex(@"\bDEBUG\s*=\s*True\b")]
    private static partial Regex DjangoDebugModePattern();

    [GeneratedRegex(@"ServerCertificateValidationCallback\s*=[^;\n]*=>\s*true\b")]
    private static partial Regex DotNetTlsDisabledPattern();

    [GeneratedRegex(@"rejectUnauthorized\s*:\s*false\b")]
    private static partial Regex NodeTlsDisabledPattern();

    [GeneratedRegex(@"verify\s*=\s*False\b")]
    private static partial Regex PythonRequestsTlsDisabledPattern();
}
