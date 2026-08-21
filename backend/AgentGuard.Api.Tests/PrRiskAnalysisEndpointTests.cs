using System.Net;
using System.Net.Http.Json;
using AgentGuard.Api.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AgentGuard.Api.Tests;

public class PrRiskAnalysisEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PrRiskAnalysisEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Returns_200_with_a_safe_to_review_result_for_a_pr_with_no_changes()
    {
        var request = new PullRequestChangeSetRequest
        {
            RepositoryName = "agentguard-demo",
            PrNumber = 1,
            PrTitle = "Update README",
            ChangedFiles = [],
        };

        var response = await _client.PostAsJsonAsync("/api/pr-risk-analysis", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();
        body.Should().NotBeNull();
        body!.Score.Should().Be(0);
        body.Classification.Should().Be("LOW");
        body.Recommendation.Should().Be("SAFE_TO_REVIEW");
        body.Checks.Should().HaveCount(5);
        body.Checks.Should().OnlyContain(c => c.Passed);
        body.Findings.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_200_with_block_merge_when_a_secret_is_detected()
    {
        var request = new PullRequestChangeSetRequest
        {
            RepositoryName = "agentguard-demo",
            PrNumber = 7,
            PrTitle = "Add debug logging",
            ChangedFiles =
            [
                new ChangedFileRequest
                {
                    Path = "src/config/aws.ts",
                    ChangeType = "ADDED",
                    NewContent = "const key = 'AKIAABCDEFGHIJKLMNOP';",
                    LinesAdded = 1,
                    LinesDeleted = 0,
                },
            ],
        };

        var response = await _client.PostAsJsonAsync("/api/pr-risk-analysis", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();
        body!.Score.Should().Be(100);
        body.Classification.Should().Be("CRITICAL");
        body.Recommendation.Should().Be("BLOCK_MERGE");

        var rawResponseText = await response.Content.ReadAsStringAsync();
        rawResponseText.Should().NotContain("AKIAABCDEFGHIJKLMNOP");
    }

    [Fact]
    public async Task Returns_400_with_validation_errors_when_required_fields_are_missing()
    {
        var request = new PullRequestChangeSetRequest
        {
            RepositoryName = null,
            PrNumber = null,
            PrTitle = null,
            ChangedFiles = null,
        };

        var response = await _client.PostAsJsonAsync("/api/pr-risk-analysis", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        body.Should().NotBeNull();
        body!.Errors.Should().NotBeEmpty();
        body.Errors.Should().Contain(e => e.Contains("repositoryName"));
    }
}
