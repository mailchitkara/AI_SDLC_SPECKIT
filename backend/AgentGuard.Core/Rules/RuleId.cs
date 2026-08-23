namespace AgentGuard.Core.Rules;

/// <summary>
/// A rule's stable identity (FR-001). Backed by the same SCREAMING_SNAKE_CASE string already used
/// at the API boundary, rather than a closed C# enum — adding rule N+1 must never require
/// modifying any existing rule's definition (research.md §1).
/// </summary>
public readonly record struct RuleId(string Value)
{
    public override string ToString() => Value;
}
