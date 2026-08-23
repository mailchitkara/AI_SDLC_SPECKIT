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
        // 6, not 5, since 006-security-risk-rules appended OVERLY_PERMISSIVE_ACCESS_CONTROL.
        body.Checks.Should().HaveCount(6);
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

        // 005-risk-engine-foundation US1 (T012): richer finding fields, regression-safe (FR-013).
        var secretFinding = body.Findings.Should().ContainSingle(f => f.RuleId == "SECRET_DETECTED").Subject;
        secretFinding.Dimension.Should().Be("SECURITY");
        secretFinding.Confidence.Should().Be("CERTAIN");
        secretFinding.Kind.Should().Be("DETERMINISTIC");
        secretFinding.MandatoryOverride.Should().BeFalse();
        // Reaches BLOCK_MERGE via score (BLOCKER weight), not a mandatory override (research.md §3).
        body.RecommendationForcedByOverride.Should().BeFalse();
    }

    // 005-risk-engine-foundation US2 (T018): configurable thresholds.

    [Fact]
    public async Task Custom_thresholds_reclassify_a_score_that_the_defaults_would_classify_differently()
    {
        var changedFiles = new List<ChangedFileRequest>
        {
            new()
            {
                Path = "src/pricing/PricingEngine.cs",
                ChangeType = "MODIFIED",
                OldContent = "x",
                NewContent = "y",
                LinesAdded = 300,
                LinesDeleted = 250,
            },
        };
        var baseRequest = new PullRequestChangeSetRequest
        {
            RepositoryName = "agentguard-demo",
            PrNumber = 42,
            PrTitle = "Refactor pricing engine",
            ChangedFiles = changedFiles,
        };

        var defaultResponse = await _client.PostAsJsonAsync("/api/pr-risk-analysis", baseRequest);
        var defaultBody = await defaultResponse.Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();

        var customRequest = baseRequest with
        {
            Thresholds = new ThresholdConfigurationRequest { LowMax = 5, MediumMax = 10, HighMax = 74 },
        };
        var customResponse = await _client.PostAsJsonAsync("/api/pr-risk-analysis", customRequest);
        var customBody = await customResponse.Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();

        customResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        customBody!.Score.Should().Be(defaultBody!.Score); // score arithmetic unchanged (FR-009)
        customBody.Classification.Should().NotBe(defaultBody.Classification); // but banding differs
    }

    [Theory]
    [InlineData(50, 20, 74)] // out of order
    [InlineData(-1, 20, 74)] // negative
    [InlineData(0, 20, 150)] // >= 100
    public async Task Invalid_thresholds_return_400(int lowMax, int mediumMax, int highMax)
    {
        var request = new PullRequestChangeSetRequest
        {
            RepositoryName = "agentguard-demo",
            PrNumber = 1,
            PrTitle = "t",
            ChangedFiles = [],
            Thresholds = new ThresholdConfigurationRequest { LowMax = lowMax, MediumMax = mediumMax, HighMax = highMax },
        };

        var response = await _client.PostAsJsonAsync("/api/pr-risk-analysis", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Partial_thresholds_return_400()
    {
        var request = new PullRequestChangeSetRequest
        {
            RepositoryName = "agentguard-demo",
            PrNumber = 1,
            PrTitle = "t",
            ChangedFiles = [],
            Thresholds = new ThresholdConfigurationRequest { LowMax = 24, MediumMax = 49 }, // HighMax missing
        };

        var response = await _client.PostAsJsonAsync("/api/pr-risk-analysis", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
