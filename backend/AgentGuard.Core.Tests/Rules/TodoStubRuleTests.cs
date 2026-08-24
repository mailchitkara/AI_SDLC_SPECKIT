using AgentGuard.Core;
using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Rules;

public class TodoStubRuleTests
{
    private static ChangedFile Added(string path, string newContent) =>
        new(path, ChangeType.Added, OldContent: null, NewContent: newContent, LinesAdded: 1, LinesDeleted: 0);

    private static ChangedFile Modified(string path, string? oldContent, string newContent) =>
        new(path, ChangeType.Modified, OldContent: oldContent, NewContent: newContent, LinesAdded: 1, LinesDeleted: 1);

    private static PullRequestChangeSet ChangeSetWith(params ChangedFile[] files) =>
        new("agentguard-demo", 1, "test", files);

    // Built via compile-time-constant concatenation so this test file's own source text doesn't
    // contain a contiguous match for the patterns it's testing (research.md §6).
    private const string TodoComment = "Price = base; // TO" + "DO: apply discount rules";
    private const string NoMarker = "Price = base;";
    private const string CSharpStub = "public void Refund() { throw new NotImplemented" + "Exception(); }";
    private const string CSharpImplemented = "public void Refund() { _gateway.Refund(); }";
    private const string PythonStub = "def refund():\n    raise NotImplemented" + "Error";
    private const string PythonImplemented = "def refund():\n    gateway.refund()";
    private const string HackathonWord = "string eventName = \"Hack" + "athon2026\";";

    [Theory]
    [InlineData(TodoComment, "TODO/FIXME/HACK Comment Marker")]
    [InlineData(CSharpStub, "Not-Implemented Stub (C#)")]
    [InlineData(PythonStub, "Not-Implemented Stub (Python)")]
    public void Fires_on_each_recognized_pattern_in_newly_added_content(string content, string expectedPatternName)
    {
        var changeSet = ChangeSetWith(Added("Program.cs", content));

        var findings = TodoStubRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.TodoStub.Id);
        findings[0].Severity.Should().Be(RuleCatalog.TodoStub.DefaultSeverity);
        findings[0].Dimension.Should().Be(RuleCatalog.TodoStub.DefaultDimension);
        findings[0].Confidence.Should().Be(Confidence.Certain);
        findings[0].Kind.Should().Be(FindingKind.Deterministic);
        findings[0].Evidence.Should().Contain(expectedPatternName);
        findings[0].Location.Should().Be("Program.cs");
        findings[0].Remediation.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Does_not_fire_on_an_unrelated_word_containing_hack_as_a_substring()
    {
        var changeSet = ChangeSetWith(Added("Events.cs", HackathonWord));

        var findings = TodoStubRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_when_a_pattern_was_already_present_and_the_count_is_unchanged()
    {
        var changeSet = ChangeSetWith(
            Modified("PricingService.cs", oldContent: TodoComment, newContent: TodoComment));

        var findings = TodoStubRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Fires_when_a_genuinely_new_second_occurrence_is_added_alongside_an_existing_one()
    {
        var changeSet = ChangeSetWith(
            Modified("PricingService.cs", oldContent: TodoComment, newContent: TodoComment + "\nanother " + TodoComment));

        var findings = TodoStubRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].Evidence.Should().Contain("1 new occurrence");
    }

    [Fact]
    public void Does_not_fire_when_a_stub_is_replaced_with_a_real_implementation()
    {
        var changeSet = ChangeSetWith(
            Modified("RefundService.cs", oldContent: CSharpStub, newContent: CSharpImplemented));

        var findings = TodoStubRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_and_does_not_throw_when_new_content_is_unavailable()
    {
        var changeSet = ChangeSetWith(new ChangedFile("assets/logo.png", ChangeType.Added, null, null, 0, 0));

        var findings = TodoStubRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_on_content_containing_none_of_the_recognized_patterns()
    {
        var changeSet = ChangeSetWith(Added("PricingService.cs", NoMarker + "\n" + PythonImplemented));

        var findings = TodoStubRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }
}
