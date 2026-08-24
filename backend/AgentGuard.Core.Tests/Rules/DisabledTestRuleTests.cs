using AgentGuard.Core;
using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Rules;

public class DisabledTestRuleTests
{
    private static ChangedFile Added(string path, string newContent) =>
        new(path, ChangeType.Added, OldContent: null, NewContent: newContent, LinesAdded: 1, LinesDeleted: 0);

    private static ChangedFile Modified(string path, string? oldContent, string newContent) =>
        new(path, ChangeType.Modified, OldContent: oldContent, NewContent: newContent, LinesAdded: 1, LinesDeleted: 1);

    private static PullRequestChangeSet ChangeSetWith(params ChangedFile[] files) =>
        new("agentguard-demo", 1, "test", files);

    // These fixtures are built via compile-time-constant concatenation rather than as single
    // literals, purely so this test file's own source text doesn't contain a contiguous match for
    // the very patterns it's testing (AgentGuard analyzes its own PRs' diffs, including test
    // files — research.md §5, and specs/006-security-risk-rules for the precedent). The
    // concatenated value passed to the rule at runtime is identical either way.
    private const string XunitSkip = "[Fact(Sk" + "ip = \"flaky\")]\npublic void Charges_card() { }";
    private const string XunitNoSkip = "[Fact]\npublic void Charges_card() { }";
    private const string JsSkipModifier = "it.sk" + "ip(\"charges the card\", () => {});";
    private const string JsNoSkip = "it(\"charges the card\", () => {});";
    private const string JsSkipPrefixed = "x" + "it(\"charges the card\", () => {});";
    private const string PytestSkip = "@pytest.mark.sk" + "ip\ndef test_charges_card(): pass";
    private const string PytestNoSkip = "def test_charges_card(): pass";
    private const string GoSkip = "func TestChargesCard(t *testing.T) { t.Sk" + "ip(\"flaky\") }";
    private const string GoNoSkip = "func TestChargesCard(t *testing.T) { }";

    [Theory]
    [InlineData(XunitSkip, "xUnit Skip Parameter")]
    [InlineData(JsSkipModifier, "JS/TS Test Skip Modifier")]
    [InlineData(JsSkipPrefixed, "JS/TS Skip-Prefixed Test Function")]
    [InlineData(PytestSkip, "Pytest Skip Decorator")]
    [InlineData(GoSkip, "Go Early-Skip Call")]
    public void Fires_on_each_recognized_pattern_in_newly_added_content(string content, string expectedPatternName)
    {
        var changeSet = ChangeSetWith(Added("Program.cs", content));

        var findings = DisabledTestRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.DisabledTest.Id);
        findings[0].Severity.Should().Be(RuleCatalog.DisabledTest.DefaultSeverity);
        findings[0].Dimension.Should().Be(RuleCatalog.DisabledTest.DefaultDimension);
        findings[0].Confidence.Should().Be(Confidence.Certain);
        findings[0].Kind.Should().Be(FindingKind.Deterministic);
        findings[0].Evidence.Should().Contain(expectedPatternName);
        findings[0].Location.Should().Be("Program.cs");
        findings[0].Remediation.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Does_not_fire_when_a_pattern_was_already_present_and_the_count_is_unchanged()
    {
        var changeSet = ChangeSetWith(
            Modified("PaymentTests.cs", oldContent: XunitSkip + " // old comment", newContent: XunitSkip + " // new comment"));

        var findings = DisabledTestRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Fires_when_a_genuinely_new_second_occurrence_is_added_alongside_an_existing_one()
    {
        // Count-based, not value-based (research.md §2) -- a second identical-text occurrence
        // must still be flagged as newly introduced.
        var changeSet = ChangeSetWith(
            Modified(
                "PaymentTests.cs",
                oldContent: XunitSkip,
                newContent: XunitSkip + "\nanother" + XunitSkip));

        var findings = DisabledTestRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].Evidence.Should().Contain("1 new occurrence");
    }

    [Fact]
    public void Does_not_fire_when_a_skip_marker_is_removed()
    {
        // A previously-skipped test being re-enabled must never be flagged (edge case: spec.md).
        var changeSet = ChangeSetWith(
            Modified("PaymentTests.cs", oldContent: XunitSkip, newContent: XunitNoSkip));

        var findings = DisabledTestRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_and_does_not_throw_when_new_content_is_unavailable()
    {
        var changeSet = ChangeSetWith(new ChangedFile("assets/logo.png", ChangeType.Added, null, null, 0, 0));

        var findings = DisabledTestRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_on_content_containing_none_of_the_recognized_patterns()
    {
        var changeSet = ChangeSetWith(Added("PaymentTests.cs", XunitNoSkip + "\n" + JsNoSkip + "\n" + PytestNoSkip + "\n" + GoNoSkip));

        var findings = DisabledTestRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }
}
