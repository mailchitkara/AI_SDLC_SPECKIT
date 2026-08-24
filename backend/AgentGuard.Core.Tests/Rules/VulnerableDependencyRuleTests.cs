using AgentGuard.Core.Dependencies;
using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Rules;

public class VulnerableDependencyRuleTests
{
    private static VulnerableDependency MakeDependency(
        ExternalSeverity severity, string? advisoryId = null, string? advisoryUrl = null) =>
        new("left-pad", "1.3.0", severity, advisoryId, advisoryUrl);

    [Theory]
    [InlineData(ExternalSeverity.Low, Severity.Low)]
    [InlineData(ExternalSeverity.Moderate, Severity.Medium)]
    [InlineData(ExternalSeverity.High, Severity.High)]
    [InlineData(ExternalSeverity.Critical, Severity.High)] // capped: never Blocker (research.md §3)
    public void Maps_each_external_severity_level_to_the_correct_AgentGuard_severity(
        ExternalSeverity externalSeverity, Severity expectedSeverity)
    {
        var findings = VulnerableDependencyRule.Evaluate([MakeDependency(externalSeverity)]);

        findings.Should().ContainSingle();
        findings[0].Severity.Should().Be(expectedSeverity);
        findings[0].RuleId.Should().Be(RuleCatalog.VulnerableDependency.Id);
        findings[0].Dimension.Should().Be(RuleCatalog.VulnerableDependency.DefaultDimension);
        findings[0].Confidence.Should().Be(Confidence.Certain);
        findings[0].Kind.Should().Be(FindingKind.Deterministic);
        findings[0].Location.Should().BeNull();
    }

    [Fact]
    public void Includes_the_advisory_id_in_evidence_when_present()
    {
        var findings = VulnerableDependencyRule.Evaluate([MakeDependency(ExternalSeverity.High, advisoryId: "GHSA-xxxx")]);

        findings[0].Evidence.Should().Contain("left-pad@1.3.0").And.Contain("GHSA-xxxx");
    }

    [Fact]
    public void Omits_advisory_details_gracefully_when_neither_id_nor_url_is_present()
    {
        var findings = VulnerableDependencyRule.Evaluate([MakeDependency(ExternalSeverity.High)]);

        findings[0].Evidence.Should().Be("left-pad@1.3.0");
    }

    [Fact]
    public void Produces_one_independent_finding_per_entry_with_no_deduplication()
    {
        var dependencies = new[]
        {
            new VulnerableDependency("left-pad", "1.3.0", ExternalSeverity.Low, null, null),
            new VulnerableDependency("event-stream", "3.3.6", ExternalSeverity.Critical, "GHSA-yyyy", null),
            new VulnerableDependency("left-pad", "1.3.0", ExternalSeverity.Low, null, null), // exact duplicate
        };

        var findings = VulnerableDependencyRule.Evaluate(dependencies);

        findings.Should().HaveCount(3);
    }

    [Fact]
    public void Produces_no_findings_for_an_empty_list()
    {
        var findings = VulnerableDependencyRule.Evaluate([]);

        findings.Should().BeEmpty();
    }
}
