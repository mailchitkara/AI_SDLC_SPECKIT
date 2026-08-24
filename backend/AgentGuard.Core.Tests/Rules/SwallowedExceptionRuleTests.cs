using AgentGuard.Core;
using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Rules;

public class SwallowedExceptionRuleTests
{
    private static ChangedFile Added(string path, string newContent) =>
        new(path, ChangeType.Added, OldContent: null, NewContent: newContent, LinesAdded: 1, LinesDeleted: 0);

    private static ChangedFile Modified(string path, string? oldContent, string newContent) =>
        new(path, ChangeType.Modified, OldContent: oldContent, NewContent: newContent, LinesAdded: 1, LinesDeleted: 1);

    private static PullRequestChangeSet ChangeSetWith(params ChangedFile[] files) =>
        new("agentguard-demo", 1, "test", files);

    // Built via compile-time-constant concatenation so this test file's own source text doesn't
    // contain a contiguous match for the patterns it's testing (research.md §5).
    private const string EmptyCatch = "try { Charge(); } ca" + "tch (Exception) { }";
    private const string HandledCatch = "try { Charge(); } ca" + "tch (Exception ex) { Log(ex); }";
    private const string BareExceptPass = "try:\n    charge()\nexce" + "pt:\n    pass";
    private const string HandledExcept = "try:\n    charge()\nexce" + "pt Exception as e:\n    log(e)";
    private const string IgnoredGoErr = "if err := Charge(); err != n" + "il {\n}";
    private const string HandledGoErr = "if err := Charge(); err != n" + "il {\n    return err\n}";

    [Theory]
    [InlineData(EmptyCatch, "Empty Catch Block")]
    [InlineData(BareExceptPass, "Bare Except With Only Pass")]
    [InlineData(IgnoredGoErr, "Ignored Error Check")]
    public void Fires_on_each_recognized_pattern_in_newly_added_content(string content, string expectedPatternName)
    {
        var changeSet = ChangeSetWith(Added("Program.cs", content));

        var findings = SwallowedExceptionRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.SwallowedException.Id);
        findings[0].Severity.Should().Be(RuleCatalog.SwallowedException.DefaultSeverity);
        findings[0].Dimension.Should().Be(RuleCatalog.SwallowedException.DefaultDimension);
        findings[0].Confidence.Should().Be(Confidence.Certain);
        findings[0].Kind.Should().Be(FindingKind.Deterministic);
        findings[0].Evidence.Should().Contain(expectedPatternName);
        findings[0].Location.Should().Be("Program.cs");
        findings[0].Remediation.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(HandledCatch)]
    [InlineData(HandledExcept)]
    [InlineData(HandledGoErr)]
    public void Does_not_fire_when_the_error_is_actually_handled(string content)
    {
        var changeSet = ChangeSetWith(Added("Program.cs", content));

        var findings = SwallowedExceptionRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_when_a_pattern_was_already_present_and_the_count_is_unchanged()
    {
        var changeSet = ChangeSetWith(
            Modified("Program.cs", oldContent: EmptyCatch + " // old", newContent: EmptyCatch + " // new"));

        var findings = SwallowedExceptionRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Fires_when_a_genuinely_new_second_occurrence_is_added_alongside_an_existing_one()
    {
        var changeSet = ChangeSetWith(
            Modified("Program.cs", oldContent: EmptyCatch, newContent: EmptyCatch + "\nanother" + EmptyCatch));

        var findings = SwallowedExceptionRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].Evidence.Should().Contain("1 new occurrence");
    }

    [Fact]
    public void Does_not_fire_when_a_swallowed_error_is_replaced_with_real_handling()
    {
        var changeSet = ChangeSetWith(
            Modified("Program.cs", oldContent: EmptyCatch, newContent: HandledCatch));

        var findings = SwallowedExceptionRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_and_does_not_throw_when_new_content_is_unavailable()
    {
        var changeSet = ChangeSetWith(new ChangedFile("assets/logo.png", ChangeType.Added, null, null, 0, 0));

        var findings = SwallowedExceptionRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_on_content_containing_none_of_the_recognized_patterns()
    {
        var changeSet = ChangeSetWith(Added("Program.cs", "Charge();"));

        var findings = SwallowedExceptionRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }
}
