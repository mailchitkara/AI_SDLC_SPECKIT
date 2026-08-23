using AgentGuard.Core;
using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Rules;

public class OverlyPermissiveAccessRuleTests
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
    // files — see specs/006-security-risk-rules/quickstart.md for the same self-reference note).
    // The concatenated value passed to the rule at runtime is identical either way.
    private const string WildcardAspNetCors = "policy." + "AllowAnyOrigin" + "();";
    private const string WildcardExpressCors = "app.use(cors({ ori" + "gin: '*' }));";
    private const string WildcardHeaderCors = "res.setHeader('Access-Control-Allow-" + "Origin', '*');";
    private const string DisabledAuthorization = "[Allow" + "Anonymous]";
    private const string WildcardAllowedHosts = "ALLOWED_HOSTS = ['" + "*']";

    [Theory]
    [InlineData(WildcardAspNetCors, "Wildcard CORS Origin (ASP.NET Core)")]
    [InlineData(WildcardExpressCors, "Wildcard CORS Origin (Express/Node cors package)")]
    [InlineData(WildcardHeaderCors, "Wildcard CORS Origin (raw header)")]
    [InlineData(DisabledAuthorization, "Disabled Authorization (AllowAnonymous attribute)")]
    [InlineData(WildcardAllowedHosts, "Wildcard Allowed Hosts (Django-style)")]
    public void Fires_on_each_recognized_pattern_in_newly_added_content(string content, string expectedPatternName)
    {
        var changeSet = ChangeSetWith(Added("Program.cs", content));

        var findings = OverlyPermissiveAccessRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.OverlyPermissiveAccess.Id);
        findings[0].Severity.Should().Be(RuleCatalog.OverlyPermissiveAccess.DefaultSeverity);
        findings[0].Dimension.Should().Be(RuleCatalog.OverlyPermissiveAccess.DefaultDimension);
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
            Modified("Program.cs", oldContent: WildcardAspNetCors + " // old comment", newContent: WildcardAspNetCors + " // new comment"));

        var findings = OverlyPermissiveAccessRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Fires_when_a_genuinely_new_second_occurrence_is_added_alongside_an_existing_one()
    {
        // Count-based, not value-based (research.md §2) -- a second identical-text occurrence
        // must still be flagged as newly introduced.
        var changeSet = ChangeSetWith(
            Modified(
                "Program.cs",
                oldContent: WildcardAspNetCors,
                newContent: WildcardAspNetCors + "\nanother" + WildcardAspNetCors));

        var findings = OverlyPermissiveAccessRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].Evidence.Should().Contain("1 new occurrence");
    }

    [Fact]
    public void Does_not_fire_when_a_pattern_is_removed()
    {
        var changeSet = ChangeSetWith(
            Modified("Program.cs", oldContent: WildcardAspNetCors, newContent: "policy.WithOrigins(\"https://example.com\");"));

        var findings = OverlyPermissiveAccessRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_and_does_not_throw_when_new_content_is_unavailable()
    {
        var changeSet = ChangeSetWith(new ChangedFile("assets/logo.png", ChangeType.Added, null, null, 0, 0));

        var findings = OverlyPermissiveAccessRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_on_content_containing_none_of_the_recognized_patterns()
    {
        var changeSet = ChangeSetWith(Added("Program.cs", "app.MapGet(\"/health\", () => Results.Ok());"));

        var findings = OverlyPermissiveAccessRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }
}
