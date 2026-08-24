using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Rules;

public class LargeNewFileRuleTests
{
    [Fact]
    public void Triggers_when_a_new_file_meets_the_threshold()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("PricingEngine.cs", "content", linesAdded: 200));

        var findings = LargeNewFileRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.LargeNewFile.Id);
        findings[0].Severity.Should().Be(Severity.Medium);
        findings[0].Dimension.Should().Be(RiskDimension.ChangeManagement);
        findings[0].Confidence.Should().Be(Confidence.Certain);
        findings[0].Kind.Should().Be(FindingKind.Deterministic);
        findings[0].Evidence.Should().Contain("200");
        findings[0].Location.Should().Be("PricingEngine.cs");
        findings[0].Remediation.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Does_not_trigger_when_a_new_file_is_below_the_threshold()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("PriceDto.cs", "content", linesAdded: 199));

        var findings = LargeNewFileRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_trigger_for_a_large_modified_file()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Modified("PricingEngine.cs", "old", "new", linesAdded: 250, linesDeleted: 240));

        var findings = LargeNewFileRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_trigger_for_a_large_deleted_file()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Deleted("LegacyEngine.cs", "content", linesDeleted: 250));

        var findings = LargeNewFileRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Produces_one_finding_per_qualifying_new_file()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("PricingEngine.cs", "content", linesAdded: 250),
            TestChangeSets.Added("DiscountEngine.cs", "content", linesAdded: 300),
            TestChangeSets.Added("PriceDto.cs", "content", linesAdded: 5));

        var findings = LargeNewFileRule.Evaluate(changeSet);

        findings.Should().HaveCount(2);
        findings.Select(f => f.Location).Should().BeEquivalentTo(["PricingEngine.cs", "DiscountEngine.cs"]);
    }
}
