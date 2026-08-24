using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Findings;

public class FindingOrderingTests
{
    private static Finding MakeFinding(string ruleId, Severity severity) =>
        new(
            RuleId: new RuleId(ruleId),
            RuleName: ruleId,
            Severity: severity,
            Explanation: "e",
            Evidence: "e",
            Location: "f.cs",
            Remediation: "r",
            Dimension: RiskDimension.ChangeManagement,
            Confidence: Confidence.Certain,
            Kind: FindingKind.Deterministic);

    [Fact]
    public void Does_not_throw_when_two_different_rules_produce_findings_of_the_same_severity()
    {
        // Regression test: RuleId is a record struct without IComparable, so a naive
        // .ThenBy(f => f.RuleId) throws via Comparer<RuleId>.Default the first time two
        // different rules' findings actually need a same-severity tie-break (only ever
        // exercised once 009-generated-file-contamination's Medium severity collided with
        // MissingRelatedTests' existing Medium severity in a real request).
        var findings = new[]
        {
            MakeFinding("GENERATED_FILE_MODIFIED", Severity.Medium),
            MakeFinding("MISSING_RELATED_TESTS", Severity.Medium),
        };

        var act = () => FindingOrdering.Stable(findings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Orders_by_severity_descending_then_by_rule_id_ascending_for_ties()
    {
        var findings = new[]
        {
            MakeFinding("MISSING_RELATED_TESTS", Severity.Medium),
            MakeFinding("SECRET_DETECTED", Severity.Blocker),
            MakeFinding("GENERATED_FILE_MODIFIED", Severity.Medium),
        };

        var ordered = FindingOrdering.Stable(findings);

        ordered.Select(f => f.RuleId.Value).Should().Equal(
            "SECRET_DETECTED",
            "GENERATED_FILE_MODIFIED",
            "MISSING_RELATED_TESTS");
    }
}
