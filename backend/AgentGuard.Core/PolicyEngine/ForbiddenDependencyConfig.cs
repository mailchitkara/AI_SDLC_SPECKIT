namespace AgentGuard.Core.PolicyEngine;

/// <summary>
/// One forbidden dependency relationship: a changed file whose path matches <see cref="From"/>
/// MUST NOT import/use anything matching <see cref="To"/>. Patterns support a trailing '*' for a
/// prefix match; otherwise matching is a case-insensitive substring containment check — deliberately
/// simple, per FR-006's "simple allow/deny style list" (research.md §5).
/// </summary>
public sealed record ForbiddenDependency(string From, string To)
{
    public bool FromMatches(string filePath) => Matches(filePath, From);

    public bool ToMatches(string importTarget) => Matches(importTarget, To);

    private static bool Matches(string value, string pattern)
    {
        if (pattern.EndsWith('*'))
        {
            return value.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
        }

        return value.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// The static, in-code (or externally supplied) set of forbidden dependency relationships used
/// by ArchitectureViolationRule. V1 ships with an empty default — a consuming team supplies its
/// own relationships (FR-006). No database, no external service (FR-021).
/// </summary>
public sealed class ForbiddenDependencyConfig
{
    public static readonly ForbiddenDependencyConfig Empty = new([]);

    public IReadOnlyList<ForbiddenDependency> Relationships { get; }

    public ForbiddenDependencyConfig(IReadOnlyList<ForbiddenDependency> relationships)
    {
        Relationships = relationships;
    }

    public IEnumerable<ForbiddenDependency> MatchingRelationships(string filePath, string importTarget) =>
        Relationships.Where(r => r.FromMatches(filePath) && r.ToMatches(importTarget));
}
