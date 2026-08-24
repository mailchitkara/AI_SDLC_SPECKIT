using AgentGuard.Core.Dependencies;

namespace AgentGuard.Api.Contracts;

/// <summary>012-vulnerable-dependency-adapter FR-001: one caller-supplied, already-identified vulnerable dependency.</summary>
public sealed record VulnerableDependencyRequest
{
    public string? PackageName { get; init; }
    public string? Version { get; init; }
    public string? Severity { get; init; }
    public string? AdvisoryId { get; init; }
    public string? AdvisoryUrl { get; init; }
}

public static class VulnerableDependencyRequestValidator
{
    /// <summary>FR-006: packageName/version required, severity must be one of the four recognized levels.</summary>
    public static IReadOnlyList<string> Validate(List<VulnerableDependencyRequest>? requests)
    {
        var errors = new List<string>();

        if (requests is null)
        {
            return errors;
        }

        for (var i = 0; i < requests.Count; i++)
        {
            var request = requests[i];

            if (string.IsNullOrWhiteSpace(request.PackageName))
            {
                errors.Add($"vulnerableDependencies[{i}].packageName is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Version))
            {
                errors.Add($"vulnerableDependencies[{i}].version is required.");
            }

            if (!EnumMappings.TryParseExternalSeverity(request.Severity, out _))
            {
                errors.Add($"vulnerableDependencies[{i}].severity must be one of LOW, MODERATE, HIGH, CRITICAL.");
            }
        }

        return errors;
    }

    public static VulnerableDependency ToVulnerableDependency(this VulnerableDependencyRequest request)
    {
        EnumMappings.TryParseExternalSeverity(request.Severity, out var severity);
        return new VulnerableDependency(
            PackageName: request.PackageName!,
            Version: request.Version!,
            Severity: severity,
            AdvisoryId: request.AdvisoryId,
            AdvisoryUrl: request.AdvisoryUrl);
    }
}
