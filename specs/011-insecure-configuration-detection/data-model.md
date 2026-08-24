# Phase 1 Data Model: Insecure Configuration Detection

No changes to any existing entity. Adds one new rule and one new pattern-definitions type.

## InsecureConfigurationPattern (new)

```csharp
public sealed record InsecureConfigurationPattern(string Name, Regex Pattern, string RemediationHint);

public static class InsecureConfigurationPatterns
{
    public static readonly IReadOnlyList<InsecureConfigurationPattern> All = [ /* 4 entries, see below */ ];
}
```

| Name | Stack (FR-001) | Pattern (illustrative) |
|---|---|---|
| Debug Mode Enabled (Django) | Django (Python) | `\bDEBUG\s*=\s*True\b` |
| TLS Certificate Validation Disabled (.NET) | .NET | `ServerCertificateValidationCallback\s*=[^;\n]*=>\s*true\b` |
| TLS Certificate Validation Disabled (Node.js) | Node.js | `rejectUnauthorized\s*:\s*false\b` |
| TLS Certificate Validation Disabled (Python requests) | Python (`requests`) | `verify\s*=\s*False\b` |

## Rule registration (RuleCatalog, changed)

```csharp
public static readonly Rule InsecureConfiguration =
    new(new RuleId("INSECURE_CONFIGURATION_INTRODUCED"), "Insecure Configuration", Severity.High, RiskDimension.Configuration);
```

Appended to `RuleCatalog.All` after `TodoStub` — preserves the existing ten rules' relative order.

## Evaluation logic (InsecureConfigurationRule.Evaluate, new)

Identical shape to `OverlyPermissiveAccessRule.Evaluate`: for each changed file with non-null `NewContent`, for each pattern, count matches in old vs new content; emit a finding when the count increases.

No new `Finding` fields.

## State / lifecycle note

None of this is persisted, matching every other rule.
