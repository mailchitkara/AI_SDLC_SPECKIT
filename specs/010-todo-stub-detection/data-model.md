# Phase 1 Data Model: Newly Introduced TODO/Stub Detection

No changes to any existing entity. Adds one new rule and one new pattern-definitions type.

## TodoStubPattern (new)

```csharp
public sealed record TodoStubPattern(string Name, Regex Pattern, string RemediationHint);

public static class TodoStubPatterns
{
    public static readonly IReadOnlyList<TodoStubPattern> All = [ /* 3 entries, see below */ ];
}
```

| Name | Language/Style (FR-001) | Pattern (illustrative) |
|---|---|---|
| TODO/FIXME/HACK Comment Marker | `//` or `#` style comments | `(?i)(//\|#)\s*(TODO\|FIXME\|HACK)\b` |
| Not-Implemented Stub (C#) | C# | `throw\s+new\s+NotImplementedException\s*\(` |
| Not-Implemented Stub (Python) | Python | `raise\s+NotImplementedError\b` |

## Rule registration (RuleCatalog, changed)

```csharp
public static readonly Rule TodoStub =
    new(new RuleId("TODO_STUB_INTRODUCED"), "Newly Introduced TODO or Stub", Severity.Medium, RiskDimension.ChangeManagement);
```

Appended to `RuleCatalog.All` after `GeneratedFileModified` — preserves the existing nine rules' relative order.

## Evaluation logic (TodoStubRule.Evaluate, new)

Identical shape to `DisabledTestRule.Evaluate`/`OverlyPermissiveAccessRule.Evaluate`/`SwallowedExceptionRule.Evaluate`: for each changed file with non-null `NewContent`, for each pattern, count matches in old vs new content; emit a finding when the count increases.

No new `Finding` fields.

## State / lifecycle note

None of this is persisted, matching every other rule.
