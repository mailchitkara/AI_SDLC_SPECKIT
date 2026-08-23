using AgentGuard.Core.PolicyEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Core.Tests.Rules;

public class ArchitectureViolationRuleTests
{
    [Fact]
    public void Empty_config_never_triggers()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("src/Ui/Component.cs", "using MyApp.Data;\nclass Component {}"));

        var findings = ArchitectureViolationRule.Evaluate(changeSet, ForbiddenDependencyConfig.Empty);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Triggers_when_a_changed_file_adds_an_import_matching_a_forbidden_relationship()
    {
        var config = new ForbiddenDependencyConfig([new ForbiddenDependency("src/Ui/", "MyApp.Data.*")]);
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("src/Ui/Component.cs", "using MyApp.Data.Repository;\nclass Component {}"));

        var findings = ArchitectureViolationRule.Evaluate(changeSet, config);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.ArchitectureViolation.Id);
        findings[0].Severity.Should().Be(AgentGuard.Core.RiskEngine.Severity.High);
        findings[0].Location.Should().Be("src/Ui/Component.cs");
    }

    [Fact]
    public void Does_not_trigger_for_an_import_that_was_already_present_before_the_change()
    {
        var config = new ForbiddenDependencyConfig([new ForbiddenDependency("src/Ui/", "MyApp.Data.*")]);
        const string existingContent = "using MyApp.Data.Repository;\nclass Component {}";
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Modified("src/Ui/Component.cs", existingContent, existingContent + "\n// comment"));

        var findings = ArchitectureViolationRule.Evaluate(changeSet, config);

        findings.Should().BeEmpty();
    }
}
