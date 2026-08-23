# Phase 1 Data Model: Newly Disabled Test Detection

No changes to any existing entity. This feature adds one new rule and one new, self-contained pattern-definitions type.

## DisabledTestPattern (new)

```csharp
public sealed record DisabledTestPattern(string Name, Regex Pattern, string RemediationHint);

public static class DisabledTestPatterns
{
    public static readonly IReadOnlyList<DisabledTestPattern> All = [ /* 5 entries, see below */ ];
}
```

| Name | Framework (FR-001) | Pattern (illustrative) |
|---|---|---|
| xUnit Skip Parameter | xUnit (.NET) | `\[(Fact\|Theory)\([^\]]*\bSkip\s*=` |
| JS/TS Test Skip Modifier | Jest/Mocha (JavaScript/TypeScript) | `\b(describe\|it\|test)\.skip\s*\(` |
| JS/TS Skip-Prefixed Test Function | Jest/Mocha (JavaScript/TypeScript) | `\bx(it\|describe\|test)\s*\(` |
| Pytest Skip Decorator | pytest (Python) | `@pytest\.mark\.skip(if)?\b` |
| Go Early-Skip Call | Go's `testing` package | `\bt\.Skip(f)?\s*\(` |

## Rule registration (RuleCatalog, changed)

```csharp
public static readonly Rule DisabledTest =
    new(new RuleId("DISABLED_TEST_INTRODUCED"), "Newly Disabled Test", Severity.High, RiskDimension.Testing);
```

Appended to `RuleCatalog.All` after `OverlyPermissiveAccess` — preserves the existing six rules' relative order (matters for any test/consumer that assumed a fixed six-element order; appending, not inserting, avoids disturbing it).

## Evaluation logic (DisabledTestRule.Evaluate, new)

```
for each changed file with NewContent != null:
    for each pattern in DisabledTestPatterns.All:
        oldCount = pattern.Matches(file.OldContent ?? "").Count
        newCount = pattern.Matches(file.NewContent).Count
        if newCount > oldCount:
            emit Finding(
                RuleId: DisabledTest.Id,
                Severity: High,
                Dimension: Testing,
                Confidence: Certain,
                Kind: Deterministic,
                Evidence: "<pattern name>: <newCount - oldCount> new occurrence(s)",
                Location: file.Path,
                Remediation: pattern.RemediationHint)
```

No new `Finding` fields — every field already exists per `005-risk-engine-foundation`'s extended model. Identical evaluation shape to `OverlyPermissiveAccessRule.Evaluate` — this rule differs only in its pattern set and rule identity.

## State / lifecycle note

None of this is persisted, matching every other rule.
