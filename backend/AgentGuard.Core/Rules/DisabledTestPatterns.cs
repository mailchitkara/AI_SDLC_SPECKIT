using System.Text.RegularExpressions;

namespace AgentGuard.Core.Rules;

public sealed record DisabledTestPattern(string Name, Regex Pattern, string RemediationHint);

/// <summary>
/// Fixed set of test-skip/ignore patterns (007-disabled-test-detection FR-001, research.md §1).
/// Deliberately narrow and text-pattern-based, mirroring PermissivePatterns' shape, rather than a
/// general test-coverage/quality analyzer (FR-008). Remediation hints deliberately avoid embedding
/// the literal matching syntax (research.md §5) — the same lesson PermissivePatterns' own
/// remediation text had to learn the hard way in 006-security-risk-rules.
/// </summary>
public static partial class DisabledTestPatterns
{
    public static readonly IReadOnlyList<DisabledTestPattern> All =
    [
        new(
            "xUnit Skip Parameter",
            XunitSkipParameterPattern(),
            "Remove the skip setting on this xUnit test and fix the underlying failure, or add a tracked reason if the skip is genuinely temporary and intentional."),
        new(
            "JS/TS Test Skip Modifier",
            JsTestSkipModifierPattern(),
            "Remove the skip modifier on this test or suite and fix the underlying failure, or file a tracked issue if the skip is genuinely temporary and intentional."),
        new(
            "JS/TS Skip-Prefixed Test Function",
            JsSkipPrefixedTestFunctionPattern(),
            "Remove the skip-prefixed variant of this test or suite function and fix the underlying failure, or file a tracked issue if the skip is genuinely temporary and intentional."),
        new(
            "Pytest Skip Decorator",
            PytestSkipDecoratorPattern(),
            "Remove the skip decorator on this pytest test and fix the underlying failure, or add a tracked reason if the skip is genuinely temporary and intentional."),
        new(
            "Go Early-Skip Call",
            GoEarlySkipCallPattern(),
            "Remove the early-skip call at the top of this Go test and fix the underlying failure, or add a tracked reason if the skip is genuinely temporary and intentional."),
    ];

    [GeneratedRegex(@"\[(Fact|Theory)\([^\]]*\bSkip\s*=")]
    private static partial Regex XunitSkipParameterPattern();

    [GeneratedRegex(@"\b(describe|it|test)\.skip\s*\(")]
    private static partial Regex JsTestSkipModifierPattern();

    [GeneratedRegex(@"\bx(it|describe|test)\s*\(")]
    private static partial Regex JsSkipPrefixedTestFunctionPattern();

    [GeneratedRegex(@"@pytest\.mark\.skip(if)?\b")]
    private static partial Regex PytestSkipDecoratorPattern();

    [GeneratedRegex(@"\bt\.Skip(f)?\s*\(")]
    private static partial Regex GoEarlySkipCallPattern();
}
