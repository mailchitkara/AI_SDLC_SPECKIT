using AgentGuard.Core.Findings;
using AgentGuard.Core.PolicyEngine;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Rules;

public class BusinessCriticalPathRuleTests
{
    [Fact]
    public void Empty_config_never_triggers()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("payments/Gateway.cs", "class Gateway {}"));

        var findings = BusinessCriticalPathRule.Evaluate(changeSet, BusinessCriticalPathConfig.Empty);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Triggers_when_a_changed_file_matches_a_configured_critical_path()
    {
        var config = new BusinessCriticalPathConfig([new BusinessCriticalPath("payments/*", "Payment Processing")]);
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("payments/Gateway.cs", "class Gateway {}"));

        var findings = BusinessCriticalPathRule.Evaluate(changeSet, config);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.BusinessCriticalPath.Id);
        findings[0].Severity.Should().Be(Severity.Medium);
        findings[0].Dimension.Should().Be(RiskDimension.BusinessCriticality);
        findings[0].Confidence.Should().Be(Confidence.Certain);
        findings[0].Kind.Should().Be(FindingKind.Deterministic);
        findings[0].Evidence.Should().Contain("Payment Processing").And.Contain("payments/*");
        findings[0].Location.Should().Be("payments/Gateway.cs");
        findings[0].Remediation.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Produces_one_finding_per_matched_pattern_when_a_file_matches_multiple_patterns()
    {
        var config = new BusinessCriticalPathConfig(
        [
            new BusinessCriticalPath("payments/*", "Payment Processing"),
            new BusinessCriticalPath("Gateway", "External Gateway Integration"),
        ]);
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("payments/Gateway.cs", "class Gateway {}"));

        var findings = BusinessCriticalPathRule.Evaluate(changeSet, config);

        findings.Should().HaveCount(2);
    }

    [Fact]
    public void Does_not_trigger_for_a_file_that_does_not_match_any_configured_pattern()
    {
        var config = new BusinessCriticalPathConfig([new BusinessCriticalPath("payments/*", "Payment Processing")]);
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("docs/README.md", "# Docs"));

        var findings = BusinessCriticalPathRule.Evaluate(changeSet, config);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Still_triggers_when_a_matching_file_is_deleted()
    {
        var config = new BusinessCriticalPathConfig([new BusinessCriticalPath("payments/*", "Payment Processing")]);
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Deleted("payments/LegacyValidator.cs", "class LegacyValidator {}"));

        var findings = BusinessCriticalPathRule.Evaluate(changeSet, config);

        findings.Should().ContainSingle();
    }

    [Fact]
    public void Triggers_again_even_when_the_file_was_already_touched_before_since_this_is_not_count_based_diffing()
    {
        var config = new BusinessCriticalPathConfig([new BusinessCriticalPath("payments/*", "Payment Processing")]);
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Modified("payments/Gateway.cs", "class Gateway { }", "class Gateway { /* tweak */ }"));

        var findings = BusinessCriticalPathRule.Evaluate(changeSet, config);

        findings.Should().ContainSingle();
    }
}
