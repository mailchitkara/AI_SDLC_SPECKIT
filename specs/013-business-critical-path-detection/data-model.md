# Phase 1 Data Model: Business-Critical Path Detection

## RiskDimension (changed)

```csharp
public enum RiskDimension
{
    Security, Testing, Compatibility, Architecture, ChangeManagement,
    Dependencies, Reliability, Configuration,
    BusinessCriticality,   // new
}
```

`EnumMappings.ToApiString` gains `RiskDimension.BusinessCriticality => "BUSINESS_CRITICALITY"`.

## BusinessCriticalPath / BusinessCriticalPathConfig (new, mirrors ForbiddenDependency/ForbiddenDependencyConfig)

```csharp
namespace AgentGuard.Core.PolicyEngine;

public sealed record BusinessCriticalPath(string PathPattern, string Label)
{
    public bool Matches(string filePath) => /* identical semantics to ForbiddenDependency.Matches */;
}

public sealed class BusinessCriticalPathConfig
{
    public static readonly BusinessCriticalPathConfig Empty = new([]);

    public IReadOnlyList<BusinessCriticalPath> Paths { get; }

    public BusinessCriticalPathConfig(IReadOnlyList<BusinessCriticalPath> paths) => Paths = paths;

    public IEnumerable<BusinessCriticalPath> MatchingPaths(string filePath) =>
        Paths.Where(p => p.Matches(filePath));
}
```

## Rule registration (RuleCatalog, changed)

```csharp
public static readonly Rule BusinessCriticalPath =
    new(new RuleId("BUSINESS_CRITICAL_PATH_TOUCHED"), "Business-Critical Path Touched", Severity.Medium, RiskDimension.BusinessCriticality);
```

Appended to `RuleCatalog.All` after `VulnerableDependency` — preserves the existing twelve rules' relative order.

## Evaluation logic (BusinessCriticalPathRule.Evaluate, new)

```
Evaluate(changeSet, config: BusinessCriticalPathConfig) -> IReadOnlyList<Finding>

for each file in changeSet.ChangedFiles:
    for each matched in config.MatchingPaths(file.Path):
        emit Finding(
            RuleId: BusinessCriticalPath.Id,
            Severity: Medium,
            Dimension: BusinessCriticality,
            Confidence: Certain,
            Kind: Deterministic,
            Evidence: "<matched.Label>: <matched.PathPattern>",
            Location: file.Path,
            Remediation: "This change touches a business-critical area (<matched.Label>) — give it additional review scrutiny.")
```

No count-based diffing (unlike `006`–`011`) — every matching file in the PR is flagged every time, regardless of whether the file was previously touched, since the risk signal here is "this PR's blast radius includes a critical area," which is true for the PR as a whole, not something that only matters on first introduction.

## `AgentGuardAnalyzer` constructor (changed)

```csharp
public AgentGuardAnalyzer(
    ForbiddenDependencyConfig? forbiddenDependencyConfig = null,
    BusinessCriticalPathConfig? businessCriticalPathConfig = null)
```

Both default to their respective `Empty` configs, matching the existing constructor's precedent exactly — purely additive, no existing call site breaks.

## State / lifecycle note

None of this is persisted, matching every other rule and `ForbiddenDependencyConfig` itself.
