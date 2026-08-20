using AgentGuard.Core;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Core.Tests;

public class DeterminismTests
{
    [Fact]
    public void Analyzing_identical_input_twice_yields_an_identical_result()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("src/config/aws.ts", "const key = 'AKIAABCDEFGHIJKLMNOP';"),
            TestChangeSets.Modified("src/Widget.cs", "old", "new"));
        var analyzer = new AgentGuardAnalyzer();

        var first = analyzer.Analyze(changeSet);
        var second = analyzer.Analyze(changeSet);

        first.Should().BeEquivalentTo(second, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Analyzing_identical_input_with_two_separate_analyzer_instances_yields_an_identical_result()
    {
        var changeSet = TestChangeSets.WithFiles(
            TestChangeSets.Added("src/File1.cs", "content", linesAdded: 600));

        var first = new AgentGuardAnalyzer().Analyze(changeSet);
        var second = new AgentGuardAnalyzer().Analyze(changeSet);

        first.Should().BeEquivalentTo(second, options => options.WithStrictOrdering());
    }
}
