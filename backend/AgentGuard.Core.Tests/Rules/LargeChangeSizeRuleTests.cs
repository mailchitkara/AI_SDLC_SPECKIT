using AgentGuard.Core.Rules;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Core.Tests.Rules;

public class LargeChangeSizeRuleTests
{
    [Fact]
    public void Does_not_trigger_at_exactly_500_lines_and_20_files()
    {
        // 20 files, 25 lines added each = 500 lines total — right at both boundaries, must not trigger.
        var files = Enumerable.Range(1, 20)
            .Select(i => TestChangeSets.Added($"src/File{i}.cs", "content", linesAdded: 25))
            .ToArray();
        var changeSet = TestChangeSets.WithFiles(files);

        var findings = LargeChangeSizeRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Triggers_when_total_lines_exceed_500()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("src/File.cs", "content", linesAdded: 501));

        var findings = LargeChangeSizeRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleId.LargeChangeSize);
        findings[0].Severity.Should().Be(AgentGuard.Core.RiskEngine.Severity.Low);
    }

    [Fact]
    public void Triggers_when_changed_file_count_exceeds_20()
    {
        var files = Enumerable.Range(1, 21)
            .Select(i => TestChangeSets.Added($"src/File{i}.cs", "content", linesAdded: 1))
            .ToArray();
        var changeSet = TestChangeSets.WithFiles(files);

        var findings = LargeChangeSizeRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
    }

    [Fact]
    public void Does_not_trigger_for_a_small_change()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("src/File.cs", "content", linesAdded: 5));

        var findings = LargeChangeSizeRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }
}
