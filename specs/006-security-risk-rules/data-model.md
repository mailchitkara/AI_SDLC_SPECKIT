# Phase 1 Data Model: Overly Permissive Access Control Detection

No changes to any existing entity. This feature adds one new rule and one new, self-contained pattern-definitions type.

## PermissivePattern (new)

```csharp
public sealed record PermissivePattern(string Name, Regex Pattern, string RemediationHint);

public static class PermissivePatterns
{
    public static readonly IReadOnlyList<PermissivePattern> All = [ /* 5 entries, see below */ ];
}
```

| Name | Category (FR-001) | Pattern (illustrative) |
|---|---|---|
| Wildcard CORS Origin (ASP.NET Core) | Wildcard CORS | `\.AllowAnyOrigin\s*\(\s*\)` |
| Wildcard CORS Origin (Express/Node `cors` package) | Wildcard CORS | `\borigin\s*:\s*['"]\*['"]` |
| Wildcard CORS Origin (raw header) | Wildcard CORS | `Access-Control-Allow-Origin['"]?\s*[:,=]\s*['"]\*['"]` |
| Disabled Authorization (AllowAnonymous attribute) | Disabled authorization | `\[AllowAnonymous\]` |
| Wildcard Allowed Hosts (Django-style) | Wildcard allowed-hosts | `ALLOWED_HOSTS\s*=\s*\[\s*['"]\*['"]\s*\]` |

## Rule registration (RuleCatalog, changed)

```csharp
public static readonly Rule OverlyPermissiveAccess =
    new(new RuleId("OVERLY_PERMISSIVE_ACCESS_CONTROL"), "Overly Permissive Access Control", Severity.High, RiskDimension.Security);
```

Appended to `RuleCatalog.All` after `SecretDetected` — preserves the original five rules' relative order (matters for any test/consumer that assumed a fixed five-element order; appending, not inserting, avoids disturbing it).

## Evaluation logic (OverlyPermissiveAccessRule.Evaluate, new)

```
for each changed file with NewContent != null:
    for each pattern in PermissivePatterns.All:
        oldCount = pattern.Matches(file.OldContent ?? "").Count
        newCount = pattern.Matches(file.NewContent).Count
        if newCount > oldCount:
            emit Finding(
                RuleId: OverlyPermissiveAccess.Id,
                Severity: High,
                Dimension: Security,
                Confidence: Certain,
                Kind: Deterministic,
                Evidence: "<pattern name>: <newCount - oldCount> new occurrence(s)",
                Location: file.Path,
                Remediation: pattern.RemediationHint)
```

No new `Finding` fields — every field already exists per `005-risk-engine-foundation`'s extended model.

## State / lifecycle note

None of this is persisted, matching every other rule.
