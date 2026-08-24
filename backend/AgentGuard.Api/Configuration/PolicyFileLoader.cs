using System.Text.Json;
using System.Text.Json.Serialization;
using AgentGuard.Core.PolicyEngine;

namespace AgentGuard.Api.Configuration;

/// <summary>015-policy-as-code: the two operator-configurable rule seams, loaded together.</summary>
public sealed record LoadedPolicy(ForbiddenDependencyConfig ForbiddenDependencies, BusinessCriticalPathConfig BusinessCriticalPaths);

/// <summary>
/// FR-001 through FR-004: loads ForbiddenDependencyConfig/BusinessCriticalPathConfig from a single
/// JSON file at service startup. A null/empty path or a missing file yields empty configs (FR-002,
/// FR-003) — no findings from either rule, identical to the service's behavior before this feature
/// existed. A present-but-malformed file throws, failing startup loudly (FR-004, research.md §3).
/// </summary>
public static class PolicyFileLoader
{
    public static LoadedPolicy Load(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new LoadedPolicy(ForbiddenDependencyConfig.Empty, BusinessCriticalPathConfig.Empty);
        }

        PolicyFileContent? content;
        try
        {
            var json = File.ReadAllText(filePath);
            content = JsonSerializer.Deserialize<PolicyFileContent>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"AgentGuard policy file at '{filePath}' could not be parsed: {ex.Message}", ex);
        }

        if (content is null)
        {
            throw new InvalidOperationException($"AgentGuard policy file at '{filePath}' is empty or null.");
        }

        var forbiddenDependencies = (content.ForbiddenDependencies ?? [])
            .Select(d => new ForbiddenDependency(d.From, d.To))
            .ToList();

        var businessCriticalPaths = (content.BusinessCriticalPaths ?? [])
            .Select(p => new BusinessCriticalPath(p.PathPattern, p.Label))
            .ToList();

        return new LoadedPolicy(
            new ForbiddenDependencyConfig(forbiddenDependencies),
            new BusinessCriticalPathConfig(businessCriticalPaths));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record PolicyFileContent(
        [property: JsonPropertyName("forbiddenDependencies")] List<ForbiddenDependencyEntry>? ForbiddenDependencies,
        [property: JsonPropertyName("businessCriticalPaths")] List<BusinessCriticalPathEntry>? BusinessCriticalPaths);

    private sealed record ForbiddenDependencyEntry(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string To);

    private sealed record BusinessCriticalPathEntry(
        [property: JsonPropertyName("pathPattern")] string PathPattern,
        [property: JsonPropertyName("label")] string Label);
}
