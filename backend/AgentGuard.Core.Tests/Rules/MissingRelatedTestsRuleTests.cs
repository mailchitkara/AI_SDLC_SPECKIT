using AgentGuard.Core.Rules;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Core.Tests.Rules;

public class MissingRelatedTestsRuleTests
{
    [Fact]
    public void Triggers_when_source_file_changes_with_no_test_file_change()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Modified("src/Widget.cs", "old", "new"));

        var findings = MissingRelatedTestsRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.MissingRelatedTests.Id);
        findings[0].Severity.Should().Be(AgentGuard.Core.RiskEngine.Severity.Medium);
    }

    [Fact]
    public void Does_not_trigger_when_a_related_test_file_also_changes()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Modified("src/Widget.cs", "old", "new"),
            TestChangeSets.Modified("tests/WidgetTests.cs", "old", "new"));

        var findings = MissingRelatedTestsRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_trigger_when_only_test_files_change()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Modified("tests/WidgetTests.cs", "old", "new"));

        var findings = MissingRelatedTestsRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_trigger_when_only_non_source_non_test_files_change()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Modified("docs/README.md", "old", "new"));

        var findings = MissingRelatedTestsRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }
}
