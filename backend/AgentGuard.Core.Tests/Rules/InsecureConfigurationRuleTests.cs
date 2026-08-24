using AgentGuard.Core;
using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;
using AgentGuard.Core.Rules;
using FluentAssertions;

namespace AgentGuard.Core.Tests.Rules;

public class InsecureConfigurationRuleTests
{
    private static ChangedFile Added(string path, string newContent) =>
        new(path, ChangeType.Added, OldContent: null, NewContent: newContent, LinesAdded: 1, LinesDeleted: 0);

    private static ChangedFile Modified(string path, string? oldContent, string newContent) =>
        new(path, ChangeType.Modified, OldContent: oldContent, NewContent: newContent, LinesAdded: 1, LinesDeleted: 1);

    private static PullRequestChangeSet ChangeSetWith(params ChangedFile[] files) =>
        new("agentguard-demo", 1, "test", files);

    // Built via compile-time-constant concatenation so this test file's own source text doesn't
    // contain a contiguous match for the patterns it's testing (research.md §6).
    private const string DjangoDebugOn = "DEBUG = Tr" + "ue";
    private const string DjangoDebugOff = "DEBUG = False";
    private const string DotNetTlsDisabled = "handler.ServerCertificateValidationCallback = (msg, cert, chain, errors) => tr" + "ue;";
    private const string DotNetTlsProper = "handler.ServerCertificateValidationCallback = (msg, cert, chain, errors) => errors == SslPolicyErrors.None;";
    private const string NodeTlsDisabled = "const agent = new https.Agent({ reject" + "Unauthorized: false });";
    private const string NodeTlsProper = "const agent = new https.Agent({});";
    private const string PythonTlsDisabled = "resp = requests.get(url, veri" + "fy=False)";
    private const string PythonTlsProper = "resp = requests.get(url)";

    [Theory]
    [InlineData(DjangoDebugOn, "Debug Mode Enabled (Django)")]
    [InlineData(DotNetTlsDisabled, "TLS Certificate Validation Disabled (.NET)")]
    [InlineData(NodeTlsDisabled, "TLS Certificate Validation Disabled (Node.js)")]
    [InlineData(PythonTlsDisabled, "TLS Certificate Validation Disabled (Python requests)")]
    public void Fires_on_each_recognized_pattern_in_newly_added_content(string content, string expectedPatternName)
    {
        var changeSet = ChangeSetWith(Added("Program.cs", content));

        var findings = InsecureConfigurationRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].RuleId.Should().Be(RuleCatalog.InsecureConfiguration.Id);
        findings[0].Severity.Should().Be(RuleCatalog.InsecureConfiguration.DefaultSeverity);
        findings[0].Dimension.Should().Be(RuleCatalog.InsecureConfiguration.DefaultDimension);
        findings[0].Confidence.Should().Be(Confidence.Certain);
        findings[0].Kind.Should().Be(FindingKind.Deterministic);
        findings[0].Evidence.Should().Contain(expectedPatternName);
        findings[0].Location.Should().Be("Program.cs");
        findings[0].Remediation.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(DjangoDebugOff)]
    [InlineData(DotNetTlsProper)]
    [InlineData(NodeTlsProper)]
    [InlineData(PythonTlsProper)]
    public void Does_not_fire_on_content_containing_none_of_the_recognized_patterns(string content)
    {
        var changeSet = ChangeSetWith(Added("Program.cs", content));

        var findings = InsecureConfigurationRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_when_a_pattern_was_already_present_and_the_count_is_unchanged()
    {
        var changeSet = ChangeSetWith(
            Modified("settings.py", oldContent: DjangoDebugOn + " # old", newContent: DjangoDebugOn + " # new"));

        var findings = InsecureConfigurationRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Fires_when_a_genuinely_new_second_occurrence_is_added_alongside_an_existing_one()
    {
        var changeSet = ChangeSetWith(
            Modified("settings.py", oldContent: DjangoDebugOn, newContent: DjangoDebugOn + "\n" + DjangoDebugOn));

        var findings = InsecureConfigurationRule.Evaluate(changeSet);

        findings.Should().ContainSingle();
        findings[0].Evidence.Should().Contain("1 new occurrence");
    }

    [Fact]
    public void Does_not_fire_when_an_insecure_setting_is_replaced_with_a_secure_one()
    {
        var changeSet = ChangeSetWith(
            Modified("settings.py", oldContent: DjangoDebugOn, newContent: DjangoDebugOff));

        var findings = InsecureConfigurationRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Does_not_fire_and_does_not_throw_when_new_content_is_unavailable()
    {
        var changeSet = ChangeSetWith(new ChangedFile("assets/logo.png", ChangeType.Added, null, null, 0, 0));

        var findings = InsecureConfigurationRule.Evaluate(changeSet);

        findings.Should().BeEmpty();
    }
}
