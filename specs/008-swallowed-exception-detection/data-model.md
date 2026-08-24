# Phase 1 Data Model: Newly Swallowed Exception Detection

No changes to any existing entity. Adds one new rule and one new pattern-definitions type.

## SwallowedExceptionPattern (new)

```csharp
public sealed record SwallowedExceptionPattern(string Name, Regex Pattern, string RemediationHint);

public static class SwallowedExceptionPatterns
{
    public static readonly IReadOnlyList<SwallowedExceptionPattern> All = [ /* 3 entries, see below */ ];
}
```

| Name | Language (FR-001) | Pattern (illustrative) |
|---|---|---|
| Empty Catch Block | C# / JavaScript / TypeScript | `catch\s*(\([^)]*\))?\s*\{\s*\}` |
| Bare Except With Only Pass | Python | `except[^:\n]*:\s*\n\s*pass\b` |
| Ignored Error Check | Go | `if\s+(?:[^{\n]*;\s*)?err\s*!=\s*nil\s*\{\s*\}` |

## Rule registration (RuleCatalog, changed)

```csharp
public static readonly Rule SwallowedException =
    new(new RuleId("SWALLOWED_EXCEPTION_INTRODUCED"), "Newly Swallowed Exception", Severity.High, RiskDimension.Reliability);
```

Appended to `RuleCatalog.All` after `DisabledTest` — preserves the existing seven rules' relative order.

## Evaluation logic (SwallowedExceptionRule.Evaluate, new)

Identical shape to `DisabledTestRule.Evaluate`/`OverlyPermissiveAccessRule.Evaluate`: for each changed file with non-null `NewContent`, for each pattern, count matches in old vs new content; emit a finding when the count increases.

No new `Finding` fields.

## State / lifecycle note

None of this is persisted, matching every other rule.
