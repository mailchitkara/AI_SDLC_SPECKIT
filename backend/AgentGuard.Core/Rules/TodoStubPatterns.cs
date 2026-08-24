using System.Text.RegularExpressions;

namespace AgentGuard.Core.Rules;

public sealed record TodoStubPattern(string Name, Regex Pattern, string RemediationHint);

/// <summary>
/// Fixed set of incompleteness patterns (010-todo-stub-detection FR-001, research.md §1).
/// Deliberately narrow and text-pattern-based, mirroring DisabledTestPatterns' shape, rather than
/// a general code-completeness analyzer (FR-008).
/// </summary>
public static partial class TodoStubPatterns
{
    public static readonly IReadOnlyList<TodoStubPattern> All =
    [
        new(
            "TODO/FIXME/HACK Comment Marker",
            CommentMarkerPattern(),
            "Finish the work or track it explicitly (an issue reference, a follow-up PR) instead of leaving an inline marker."),
        new(
            "Not-Implemented Stub (C#)",
            CSharpStubPattern(),
            "Implement this method, or leave it unimplemented only as a deliberately tracked, temporary placeholder."),
        new(
            "Not-Implemented Stub (Python)",
            PythonStubPattern(),
            "Implement this function, or leave it unimplemented only as a deliberately tracked, temporary placeholder."),
    ];

    [GeneratedRegex(@"(?i)(//|#)\s*(TODO|FIXME|HACK)\b")]
    private static partial Regex CommentMarkerPattern();

    [GeneratedRegex(@"throw\s+new\s+NotImplementedException\s*\(")]
    private static partial Regex CSharpStubPattern();

    [GeneratedRegex(@"raise\s+NotImplementedError\b")]
    private static partial Regex PythonStubPattern();
}
