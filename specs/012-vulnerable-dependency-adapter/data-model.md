# Phase 1 Data Model: Vulnerable Dependency Adapter

## VulnerableDependency / ExternalSeverity (new, AgentGuard.Core)

```csharp
namespace AgentGuard.Core.Dependencies;

public enum ExternalSeverity { Low, Moderate, High, Critical }

public sealed record VulnerableDependency(
    string PackageName,
    string Version,
    ExternalSeverity Severity,
    string? AdvisoryId,
    string? AdvisoryUrl);
```

## Rule registration (RuleCatalog, changed)

```csharp
public static readonly Rule VulnerableDependency =
    new(new RuleId("VULNERABLE_DEPENDENCY_DETECTED"), "Vulnerable Dependency", Severity.High, RiskDimension.Dependencies);
```

Appended to `RuleCatalog.All` after `InsecureConfiguration` — preserves the existing eleven rules' relative order. `DefaultSeverity` here is nominal (used only for the `Rule` record's own descriptive shape); the actual per-finding severity is computed from each entry's `ExternalSeverity` (research.md §3), not read from this default.

## Evaluation logic (VulnerableDependencyRule.Evaluate, new)

```
Evaluate(vulnerableDependencies: IReadOnlyList<VulnerableDependency>) -> IReadOnlyList<Finding>

for each entry in vulnerableDependencies:
    emit Finding(
        RuleId: VulnerableDependency.Id,
        Severity: MapSeverity(entry.Severity),   // Low->Low, Moderate->Medium, High->High, Critical->High
        Dimension: Dependencies,
        Confidence: Certain,
        Kind: Deterministic,
        Evidence: "<packageName>@<version>" + (advisoryId or advisoryUrl, if present),
        Location: null,   // no single file — matches LargeChangeSizeRule's precedent for PR-wide findings
        Remediation: "Upgrade to a patched version per the linked advisory.")
```

Unlike `006`–`011`, this rule takes the caller-supplied list directly, not a `PullRequestChangeSet` — it never inspects `ChangedFiles`.

## API contract addition (AgentGuard.Api)

```csharp
public sealed record VulnerableDependencyRequest
{
    public string? PackageName { get; init; }
    public string? Version { get; init; }
    public string? Severity { get; init; }       // "LOW" | "MODERATE" | "HIGH" | "CRITICAL"
    public string? AdvisoryId { get; init; }
    public string? AdvisoryUrl { get; init; }
}
```

Added as `List<VulnerableDependencyRequest>? VulnerableDependencies` on both `PullRequestChangeSetRequest` and `PrReferenceAnalysisRequest` — optional, defaults to omitted/empty, exactly mirroring `Thresholds`'s existing shape on both DTOs.

## State / lifecycle note

None of this is persisted, matching every other rule. The supplied list only exists for the duration of one analysis request.
