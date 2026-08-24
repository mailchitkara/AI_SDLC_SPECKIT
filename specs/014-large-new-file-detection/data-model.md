# Phase 1 Data Model: Large New File Detection

No changes to any existing entity. Adds one new rule only — no new types, no new dimension.

## Rule registration (RuleCatalog, changed)

```csharp
public static readonly Rule LargeNewFile =
    new(new RuleId("LARGE_NEW_FILE_INTRODUCED"), "Large New File Introduced", Severity.Medium, RiskDimension.ChangeManagement);
```

Appended to `RuleCatalog.All` after `BusinessCriticalPath` — preserves the existing thirteen rules' relative order.

## Evaluation logic (LargeNewFileRule.Evaluate, new)

```
const int LineThreshold = 200;

for each file in changeSet.ChangedFiles:
    if file.ChangeType != Added: continue
    if file.LinesAdded < LineThreshold: continue

    emit Finding(
        RuleId: LargeNewFile.Id,
        Severity: Medium,
        Dimension: ChangeManagement,
        Confidence: Certain,
        Kind: Deterministic,
        Evidence: "<LinesAdded> lines in a newly-added file",
        Location: file.Path,
        Remediation: "This file has no prior review or production history — consider extra scrutiny, or splitting it if it bundles multiple unrelated concerns.")
```

Mirrors `LargeChangeSizeRule`'s shape (a fixed, named threshold constant, no configuration), applied per-file instead of PR-wide.

## State / lifecycle note

None of this is persisted, matching every other rule.
