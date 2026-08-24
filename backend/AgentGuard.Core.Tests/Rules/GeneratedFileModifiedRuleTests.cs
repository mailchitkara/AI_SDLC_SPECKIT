using AgentGuard.Core;
using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Rules;

public class GeneratedFileModifiedRuleTests
{
    private static ChangedFile Modified(string path, string? oldContent, string newContent) =>
        new(path, ChangeType.Modified, OldContent: oldContent, NewContent: newContent, LinesAdded: 1, LinesDeleted: 1);

    private static ChangedFile Added(string path, string newContent) =>
        new(path, ChangeType.Added, OldContent: null, NewContent: newContent, LinesAdded: 1, LinesDeleted: 0);

    private static PullRequestChangeSet ChangeSetWith(params ChangedFile[] files) =>
        new("agentguard-demo", 1, "test", files);

    [Fact]
    public void Fires_when_a_recognized_generated_extension_file_is_modified_with_changed_content()
    {
        var changeSet = ChangeSetWith(
            Modified("Models/User.generated.cs", "public string Name { get; set; }", "public string Name { get; set; } = \"\";"));

        var findings = GeneratedFileModifiedRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.GeneratedFileModified.Id);
        findings[0].Severity.Should().Be(RuleCatalog.GeneratedFileModified.DefaultSeverity);
        findings[0].Dimension.Should().Be(RuleCatalog.GeneratedFileModified.DefaultDimension);
        findings[0].Confidence.Should().Be(Confidence.Certain);
        findings[0].Kind.Should().Be(FindingKind.Deterministic);
        findings[0].Evidence.Should().Contain("Extension");
        findings[0].Location.Should().Be("Models/User.generated.cs");
        findings[0].Remediation.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Fires_when_a_file_with_an_auto_generated_marker_is_modified_with_changed_content()
    {
        var markerLine = "// <" + "auto-generated" + ">";
        var changeSet = ChangeSetWith(
            Modified("Migrations/Snapshot.cs", markerLine + "\nclass Snapshot { }", markerLine + "\nclass Snapshot { public int Version = 2; }"));

        var findings = GeneratedFileModifiedRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].Evidence.Should().Contain("Marker");
    }

    [Fact]
    public void Fires_twice_when_a_file_matches_both_signals()
    {
        var markerLine = "// <" + "auto-generated" + ">";
        var changeSet = ChangeSetWith(
            Modified("Client.generated.cs", markerLine + "\nclass Client { }", markerLine + "\nclass Client { public int V = 2; }"));

        var findings = GeneratedFileModifiedRule.Evaluate(changeSet);

        findings.Should().HaveCount(2);
        findings.Select(f => f.Evidence).Should().BeEquivalentTo(["Recognized Generated-File Extension", "Auto-Generated File Marker"]);
    }

    [Fact]
    public void Does_not_fire_when_a_recognized_generated_file_is_newly_added()
    {
        var markerLine = "// <" + "auto-generated" + ">";
        var changeSet = ChangeSetWith(Added("Client.generated.cs", markerLine + "\nclass Client { }"));

        var findings = GeneratedFileModifiedRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_when_a_recognized_generated_file_is_modified_but_content_is_unchanged()
    {
        var changeSet = ChangeSetWith(
            Modified("Models/User.generated.cs", "public string Name { get; set; }", "public string Name { get; set; }"));

        var findings = GeneratedFileModifiedRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_on_an_ordinary_source_file()
    {
        var changeSet = ChangeSetWith(Modified("Services/PaymentService.cs", "x", "y"));

        var findings = GeneratedFileModifiedRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_and_does_not_throw_when_content_is_unavailable()
    {
        var changeSet = ChangeSetWith(new ChangedFile("assets/logo.png", ChangeType.Modified, null, null, 0, 0));

        var findings = GeneratedFileModifiedRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }
}
