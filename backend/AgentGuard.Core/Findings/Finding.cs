using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;

namespace AgentGuard.Core.Findings;

/// <summary>
/// An issue detected by a rule. For SecretDetected findings, Evidence MUST already be masked
/// by the caller before this record is constructed (FR-010 from 001-pr-risk-analysis-v1) —
/// there is no separate redaction step.
/// </summary>
public sealed record Finding(
    RuleId RuleId,
    string RuleName,
    Severity Severity,
    string Explanation,
    string Evidence,
    string? Location,
    string Remediation,
    RiskDimension Dimension,
    Confidence Confidence,
    FindingKind Kind,
    bool MandatoryOverride = false);

public sealed record CheckResult(RuleId RuleId, string RuleName, bool Passed);
