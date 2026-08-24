namespace AgentGuard.Core.PolicyEngine;

/// <summary>
/// One business-critical path pattern: a changed file whose path matches <see cref="PathPattern"/>
/// is in a business-critical area labeled <see cref="Label"/>. Matching semantics are identical to
/// <see cref="ForbiddenDependency"/>'s — a trailing '*' for a prefix match; otherwise a
/// case-insensitive substring containment check (research.md §1).
/// </summary>
public sealed record BusinessCriticalPath(string PathPattern, string Label)
{
    public bool Matches(string filePath)
    {
        if (PathPattern.EndsWith('*'))
        {
            return filePath.StartsWith(PathPattern[..^1], StringComparison.OrdinalIgnoreCase);
        }

        return filePath.Contains(PathPattern, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// The static, in-code (or externally supplied) set of business-critical path patterns used by
/// BusinessCriticalPathRule. Ships with an empty default — a consuming team supplies its own
/// patterns (FR-002); no fabricated or inferred criticality when none is configured (FR-009).
/// </summary>
public sealed class BusinessCriticalPathConfig
{
    public static readonly BusinessCriticalPathConfig Empty = new([]);

    public IReadOnlyList<BusinessCriticalPath> Paths { get; }

    public BusinessCriticalPathConfig(IReadOnlyList<BusinessCriticalPath> paths)
    {
        Paths = paths;
    }

    public IEnumerable<BusinessCriticalPath> MatchingPaths(string filePath) =>
        Paths.Where(p => p.Matches(filePath));
}
