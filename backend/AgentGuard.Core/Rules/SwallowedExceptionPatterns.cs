using System.Text.RegularExpressions;

namespace AgentGuard.Core.Rules;

public sealed record SwallowedExceptionPattern(string Name, Regex Pattern, string RemediationHint);

/// <summary>
/// Fixed set of swallowed-error patterns (008-swallowed-exception-detection FR-001, research.md §1).
/// Deliberately narrow and text-pattern-based, mirroring DisabledTestPatterns' shape, rather than
/// a general control-flow analyzer (FR-008). Matches a genuinely empty (whitespace-only) handler
/// body only — a comment-only body is an accepted limitation (spec.md edge cases).
/// </summary>
public static partial class SwallowedExceptionPatterns
{
    public static readonly IReadOnlyList<SwallowedExceptionPattern> All =
    [
        new(
            "Empty Catch Block",
            EmptyCatchBlockPattern(),
            "Handle, log, or propagate the error instead of leaving the catch block empty."),
        new(
            "Bare Except With Only Pass",
            BareExceptWithPassPattern(),
            "Handle, log, or re-raise the error instead of leaving the except clause as a no-op."),
        new(
            "Ignored Error Check",
            IgnoredErrorCheckPattern(),
            "Handle, log, or return the error instead of leaving the error-check body empty."),
    ];

    [GeneratedRegex(@"catch\s*(\([^)]*\))?\s*\{\s*\}")]
    private static partial Regex EmptyCatchBlockPattern();

    [GeneratedRegex(@"except[^:\n]*:\s*\n\s*pass\b")]
    private static partial Regex BareExceptWithPassPattern();

    // The optional non-capturing prefix covers Go's common inline-assignment error-check idiom,
    // not just the bare unassigned form (see data-model.md for both illustrative shapes).
    [GeneratedRegex(@"if\s+(?:[^{\n]*;\s*)?err\s*!=\s*nil\s*\{\s*\}")]
    private static partial Regex IgnoredErrorCheckPattern();
}
