namespace AgentGuard.Core.Dependencies;

/// <summary>
/// The four severity levels an external dependency-vulnerability scanner (npm audit, GitHub
/// Security Advisories) commonly reports (012-vulnerable-dependency-adapter research.md §3).
/// </summary>
public enum ExternalSeverity
{
    Low,
    Moderate,
    High,
    Critical,
}

/// <summary>
/// One already-identified vulnerable dependency, supplied by the caller (research.md §1) —
/// AgentGuard never resolves a dependency tree or queries a vulnerability database itself.
/// </summary>
public sealed record VulnerableDependency(
    string PackageName,
    string Version,
    ExternalSeverity Severity,
    string? AdvisoryId,
    string? AdvisoryUrl);
