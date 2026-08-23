# Phase 1 Data Model: Risk Engine Foundation

All changes are additive to `AgentGuard.Core`'s existing model (`Finding`, `Rule`, `RiskAnalysisResult`) unless noted. Existing fields are unchanged in meaning.

## RuleId (changed: enum → stable identity)

```csharp
public readonly record struct RuleId(string Value)
{
    public override string ToString() => Value;
}
```

The five existing rules keep their exact current wire-format strings: `LARGE_CHANGE_SIZE`, `MISSING_RELATED_TESTS`, `API_CONTRACT_BREAKING_CHANGE`, `ARCHITECTURE_VIOLATION`, `SECRET_DETECTED` (FR-001, FR-013).

## RiskDimension (new)

```csharp
public enum RiskDimension
{
    Security, Testing, Compatibility, Architecture,
    ChangeManagement, Dependencies, Reliability, Configuration,
}
```

Per Clarifications: 8 values, anticipating later phases' stated rule areas. `Dependencies`, `Reliability`, `Configuration` have no rule assigned yet in this phase (FR-002).

Existing-rule mapping (FR-003):

| Rule | Dimension |
|---|---|
| LargeChangeSize | ChangeManagement |
| MissingRelatedTests | Testing |
| ApiContractBreakingChange | Compatibility |
| ArchitectureViolation | Architecture |
| SecretDetected | Security |

## Confidence (new)

```csharp
public enum Confidence { Certain, High, Medium, Low }
```

Per Clarifications. All five existing rules always produce `Certain` (FR-005).

## FindingKind (new)

```csharp
public enum FindingKind { Deterministic, Contextual }
```

All five existing rules always produce `Deterministic` (FR-004). No rule in this phase produces `Contextual` — the value exists so a later phase's rule can use it without another model change.

## ThresholdConfiguration (new)

```csharp
public sealed record ThresholdConfiguration(int LowMax, int MediumMax, int HighMax)
{
    public static readonly ThresholdConfiguration Default = new(24, 49, 74);
}
```

Bands: `score <= LowMax` → Low; `<= MediumMax` → Medium; `<= HighMax` → High; above `HighMax` → Critical. `Default` reproduces V1's fixed bands exactly (FR-007).

**Validity** (checked at the API layer, per research.md §4): `0 <= LowMax < MediumMax < HighMax < 100`. Anything else is rejected with `400` before reaching `RiskEngine` (FR-008).

Per Clarifications, this is supplied per-request only — no persisted or server-wide configuration exists.

## Finding (changed)

```csharp
public sealed record Finding(
    RuleId RuleId,
    string RuleName,
    Severity Severity,
    string Explanation,
    string Evidence,
    string? Location,
    string Remediation,
    RiskDimension Dimension,        // new
    Confidence Confidence,          // new
    FindingKind Kind,                // new
    bool MandatoryOverride = false); // new, per-finding per Clarifications
```

Every existing field's meaning is unchanged (FR-006).

## Rule (changed)

```csharp
public sealed record Rule(RuleId Id, string Name, Severity DefaultSeverity, RiskDimension DefaultDimension);
```

`RuleCatalog.All` remains the fixed, ordered list of the five rules (unchanged order, per existing FR-011 from `001-pr-risk-analysis-v1`).

## ScoredRisk / RiskAnalysisResult (changed)

```csharp
public readonly record struct ScoredRisk(
    int Score, RiskClassification Classification, Recommendation Recommendation,
    bool RecommendationForcedByOverride); // new

public sealed record RiskAnalysisResult(
    string RepositoryName, int PrNumber, string PrTitle,
    int Score, RiskClassification Classification, Recommendation Recommendation,
    bool RecommendationForcedByOverride,           // new — FR-011, FR-016
    IReadOnlyList<CheckResult> Checks,
    IReadOnlyList<Finding> Findings);
```

`RecommendationForcedByOverride` is `true` iff at least one finding has `MandatoryOverride: true` (FR-010, FR-012). Which finding(s) caused it is derivable by the caller filtering `Findings` for `MandatoryOverride: true` — no separate list is needed.

## Evaluation logic (RiskEngine.Evaluate, changed)

```
score = min(100, sum(SeverityWeights[f.Severity] for f in findings))          // unchanged (FR-009)
classification = band(score, thresholds ?? ThresholdConfiguration.Default)     // FR-007
forced = any(f.MandatoryOverride for f in findings)                            // FR-010
recommendation = BlockMerge if forced else RecommendationFor(classification)   // FR-010, FR-012
```

## API contract additions

`ThresholdConfigurationRequest` (optional field on both existing request DTOs):

```json
{ "lowMax": 24, "mediumMax": 49, "highMax": 74 }
```

`FindingResponse` gains `dimension`, `confidence`, `kind`, `mandatoryOverride`. `RiskAnalysisResultResponse` gains `recommendationForcedByOverride`. All new enum values serialize as SCREAMING_SNAKE_CASE strings, matching the existing convention (e.g. `"CHANGE_MANAGEMENT"`, `"CERTAIN"`, `"DETERMINISTIC"`).

## State / lifecycle note

None of this is persisted. A `ThresholdConfiguration` lives only for the duration of the one request that supplied it, exactly like every other input to `AgentGuardAnalyzer.Analyze` today.
