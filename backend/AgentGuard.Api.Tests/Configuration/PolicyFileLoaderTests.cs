using AgentGuard.Api.Configuration;
using FluentAssertions;

namespace AgentGuard.Api.Tests.Configuration;

public class PolicyFileLoaderTests
{
    [Fact]
    public void Loads_both_sections_from_a_well_formed_file()
    {
        var path = WriteTempFile("""
            {
              "forbiddenDependencies": [{ "from": "src/Ui/", "to": "MyApp.Data.*" }],
              "businessCriticalPaths": [{ "pathPattern": "payments/*", "label": "Payment Processing" }]
            }
            """);

        var policy = PolicyFileLoader.Load(path);

        policy.ForbiddenDependencies.Relationships.Should().ContainSingle();
        policy.ForbiddenDependencies.Relationships[0].From.Should().Be("src/Ui/");
        policy.ForbiddenDependencies.Relationships[0].To.Should().Be("MyApp.Data.*");
        policy.BusinessCriticalPaths.Paths.Should().ContainSingle();
        policy.BusinessCriticalPaths.Paths[0].PathPattern.Should().Be("payments/*");
        policy.BusinessCriticalPaths.Paths[0].Label.Should().Be("Payment Processing");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Returns_both_configs_empty_for_a_null_or_blank_path(string? path)
    {
        var policy = PolicyFileLoader.Load(path);

        policy.ForbiddenDependencies.Relationships.Should().BeEmpty();
        policy.BusinessCriticalPaths.Paths.Should().BeEmpty();
    }

    [Fact]
    public void Returns_both_configs_empty_for_a_path_that_does_not_exist_without_throwing()
    {
        var act = () => PolicyFileLoader.Load(@"C:\definitely\does\not\exist\policy.json");

        var policy = act.Should().NotThrow().Subject;
        policy.ForbiddenDependencies.Relationships.Should().BeEmpty();
        policy.BusinessCriticalPaths.Paths.Should().BeEmpty();
    }

    [Fact]
    public void Throws_a_clear_error_for_malformed_json()
    {
        var path = WriteTempFile("{ this is not valid json");

        var act = () => PolicyFileLoader.Load(path);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{path}*");
    }

    [Fact]
    public void Throws_a_clear_error_when_a_section_has_the_wrong_shape()
    {
        var path = WriteTempFile("""{ "forbiddenDependencies": "this should be an array" }""");

        var act = () => PolicyFileLoader.Load(path);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Leaves_the_other_section_empty_when_only_one_is_present()
    {
        var path = WriteTempFile("""
            { "businessCriticalPaths": [{ "pathPattern": "payments/*", "label": "Payment Processing" }] }
            """);

        var policy = PolicyFileLoader.Load(path);

        policy.ForbiddenDependencies.Relationships.Should().BeEmpty();
        policy.BusinessCriticalPaths.Paths.Should().ContainSingle();
    }

    [Fact]
    public void Ignores_unrecognized_extra_fields_rather_than_failing()
    {
        var path = WriteTempFile("""
            {
              "forbiddenDependencies": [],
              "businessCriticalPaths": [],
              "someFuturePolicySection": { "whatever": true }
            }
            """);

        var act = () => PolicyFileLoader.Load(path);

        act.Should().NotThrow();
    }

    private static string WriteTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"agentguard-policy-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }
}
