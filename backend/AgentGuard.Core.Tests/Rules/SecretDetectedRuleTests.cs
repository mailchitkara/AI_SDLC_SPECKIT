using AgentGuard.Core.Rules;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Core.Tests.Rules;

public class SecretDetectedRuleTests
{
    private const string FixtureSecret = "AKIAABCDEFGHIJKLMNOP";

    [Fact]
    public void Triggers_a_blocker_finding_when_a_recognized_secret_pattern_is_introduced()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("src/config/aws.ts", $"const key = '{FixtureSecret}';"));

        var findings = SecretDetectedRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleId.SecretDetected);
        findings[0].Severity.Should().Be(AgentGuard.Core.RiskEngine.Severity.Blocker);
    }

    [Fact]
    public void Never_exposes_the_raw_secret_value_in_the_findings_evidence()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("src/config/aws.ts", $"const key = '{FixtureSecret}';"));

        var findings = SecretDetectedRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].Evidence.Should().NotContain(FixtureSecret);
        findings[0].Evidence.Should().NotBe(FixtureSecret);
    }

    [Fact]
    public void Does_not_retrigger_for_a_secret_that_was_already_present_before_the_change()
    {
        var unchangedContent = $"const key = '{FixtureSecret}';";
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Modified("src/config/aws.ts", unchangedContent, unchangedContent + "\n// unrelated comment"));

        var findings = SecretDetectedRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_trigger_for_content_with_no_recognized_secret_pattern()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("src/App.tsx", "export function App() { return null; }"));

        var findings = SecretDetectedRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }
}
