using System.Net;
using System.Net.Http.Json;
using AgentGuard.Api.Contracts;
using AgentGuard.Api.GitHub;
using AgentGuard.Api.Tests.Fakes;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AgentGuard.Api.Tests;

public class PrReferenceAnalysisEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Endpoint = "/api/pr-risk-analysis/from-reference";
    private readonly WebApplicationFactory<Program> _factory;

    public PrReferenceAnalysisEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient ClientWithFakeGitHub(FakeGitHubPullRequestClient fake) =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGitHubPullRequestClient>();
                services.AddSingleton<IGitHubPullRequestClient>(fake);
            }))
            .CreateClient();

    private static GitHubPullRequestSuccess CleanPrSuccess() => new(
        PrTitle: "Update README",
        Files:
        [
            new RetrievedFile("README.md", "modified", "old", "new", LinesAdded: 1, LinesDeleted: 1, FullyEvaluated: true),
        ]);

    // --- User Story 1: analyze by URL or by owner/repository/prNumber, deterministically ---

    [Fact]
    public async Task PrUrl_form_returns_200_with_expected_shape()
    {
        var fake = new FakeGitHubPullRequestClient(GitHubPullRequestClientResult.SuccessResult(CleanPrSuccess()));
        var client = ClientWithFakeGitHub(fake);

        var response = await client.PostAsJsonAsync(Endpoint, new PrReferenceAnalysisRequest
        {
            PrUrl = "https://github.com/chalk/chalk/pull/688",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();
        body.Should().NotBeNull();
        body!.RepositoryName.Should().Be("chalk");
        body.PrNumber.Should().Be(688);
        body.Score.Should().Be(0);
        body.Classification.Should().Be("LOW");
        body.Recommendation.Should().Be("SAFE_TO_REVIEW");
        body.PartiallyEvaluatedFiles.Should().BeEmpty();
        fake.Calls.Should().ContainSingle(c => c.Owner == "chalk" && c.Repository == "chalk" && c.PrNumber == 688);
    }

    [Fact]
    public async Task Owner_repository_prNumber_trio_form_returns_identical_result_to_prUrl_form()
    {
        var fakeForUrl = new FakeGitHubPullRequestClient(GitHubPullRequestClientResult.SuccessResult(CleanPrSuccess()));
        var urlResponse = await ClientWithFakeGitHub(fakeForUrl).PostAsJsonAsync(Endpoint, new PrReferenceAnalysisRequest
        {
            PrUrl = "https://github.com/chalk/chalk/pull/688",
        });
        var urlBody = await urlResponse.Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();

        var fakeForTrio = new FakeGitHubPullRequestClient(GitHubPullRequestClientResult.SuccessResult(CleanPrSuccess()));
        var trioResponse = await ClientWithFakeGitHub(fakeForTrio).PostAsJsonAsync(Endpoint, new PrReferenceAnalysisRequest
        {
            Owner = "chalk",
            Repository = "chalk",
            PrNumber = 688,
        });
        var trioBody = await trioResponse.Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();

        trioResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        trioBody.Should().BeEquivalentTo(urlBody);
    }

    [Fact]
    public async Task Same_request_run_twice_produces_identical_result()
    {
        var fake = new FakeGitHubPullRequestClient(GitHubPullRequestClientResult.SuccessResult(CleanPrSuccess()));
        var client = ClientWithFakeGitHub(fake);
        var request = new PrReferenceAnalysisRequest { PrUrl = "https://github.com/chalk/chalk/pull/688" };

        var first = await (await client.PostAsJsonAsync(Endpoint, request)).Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();
        var second = await (await client.PostAsJsonAsync(Endpoint, request)).Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();

        second.Should().BeEquivalentTo(first);
    }

    // --- invalid reference (400, no retry offered) ---

    [Theory]
    [InlineData(null, null, null, null)] // neither form
    [InlineData("https://github.com/a/b/pull/1", "a", "b", 1)] // both forms
    [InlineData("not a url", null, null, null)] // malformed prUrl
    public async Task Invalid_reference_shapes_return_400(string? prUrl, string? owner, string? repo, int? prNumber)
    {
        var fake = new FakeGitHubPullRequestClient(GitHubPullRequestClientResult.SuccessResult(CleanPrSuccess()));
        var client = ClientWithFakeGitHub(fake);

        var response = await client.PostAsJsonAsync(Endpoint, new PrReferenceAnalysisRequest
        {
            PrUrl = prUrl,
            Owner = owner,
            Repository = repo,
            PrNumber = prNumber,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ImportErrorResponse>();
        body!.ErrorType.Should().Be("invalid_reference");
        body.RetryableWithCredential.Should().BeFalse();
        fake.Calls.Should().BeEmpty("an invalid reference must be rejected before any GitHub call");
    }

    // --- User Story 3: not-found-or-no-access, recoverable via credential retry ---

    [Fact]
    public async Task NotFoundOrNoAccess_returns_404_retryable_with_credential()
    {
        var fake = new FakeGitHubPullRequestClient(GitHubPullRequestClientResult.NotFoundOrNoAccessResult());
        var client = ClientWithFakeGitHub(fake);

        var response = await client.PostAsJsonAsync(Endpoint, new PrReferenceAnalysisRequest
        {
            PrUrl = "https://github.com/some-org/private-repo/pull/1",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ImportErrorResponse>();
        body!.ErrorType.Should().Be("not_found_or_no_access");
        body.RetryableWithCredential.Should().BeTrue();
    }

    [Fact]
    public async Task Retry_with_a_credential_that_has_access_succeeds()
    {
        var fake = new FakeGitHubPullRequestClient(credential => credential == "valid-token"
            ? GitHubPullRequestClientResult.SuccessResult(CleanPrSuccess())
            : GitHubPullRequestClientResult.NotFoundOrNoAccessResult());
        var client = ClientWithFakeGitHub(fake);
        var request = new PrReferenceAnalysisRequest { PrUrl = "https://github.com/some-org/private-repo/pull/1" };

        var firstAttempt = await client.PostAsJsonAsync(Endpoint, request);
        var retry = await client.PostAsJsonAsync(Endpoint, request with { Credential = "valid-token" });

        firstAttempt.StatusCode.Should().Be(HttpStatusCode.NotFound);
        retry.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Retry_with_a_credential_that_still_lacks_access_returns_the_same_outcome()
    {
        var fake = new FakeGitHubPullRequestClient(_ => GitHubPullRequestClientResult.NotFoundOrNoAccessResult());
        var client = ClientWithFakeGitHub(fake);
        var request = new PrReferenceAnalysisRequest { PrUrl = "https://github.com/some-org/private-repo/pull/1" };

        var firstAttempt = await client.PostAsJsonAsync(Endpoint, request);
        var retry = await client.PostAsJsonAsync(Endpoint, request with { Credential = "still-no-access" });

        retry.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await retry.Content.ReadFromJsonAsync<ImportErrorResponse>();
        body!.ErrorType.Should().Be("not_found_or_no_access");
    }

    // --- rate limiting ---

    [Fact]
    public async Task RateLimited_returns_429()
    {
        var fake = new FakeGitHubPullRequestClient(GitHubPullRequestClientResult.RateLimitedResult("120"));
        var client = ClientWithFakeGitHub(fake);

        var response = await client.PostAsJsonAsync(Endpoint, new PrReferenceAnalysisRequest
        {
            PrUrl = "https://github.com/chalk/chalk/pull/688",
        });

        response.StatusCode.Should().Be((HttpStatusCode)429);
        var body = await response.Content.ReadFromJsonAsync<ImportErrorResponse>();
        body!.ErrorType.Should().Be("rate_limited");
        response.Headers.RetryAfter.Should().NotBeNull();
    }

    // --- User Story 2: partially-evaluated files don't block the rest of the analysis ---

    [Fact]
    public async Task A_file_that_could_not_be_retrieved_is_reported_but_does_not_block_analysis()
    {
        var success = new GitHubPullRequestSuccess(
            PrTitle: "Add a logo",
            Files:
            [
                new RetrievedFile("assets/logo.png", "added", null, null, LinesAdded: 0, LinesDeleted: 0, FullyEvaluated: false),
            ]);
        var fake = new FakeGitHubPullRequestClient(GitHubPullRequestClientResult.SuccessResult(success));
        var client = ClientWithFakeGitHub(fake);

        var response = await client.PostAsJsonAsync(Endpoint, new PrReferenceAnalysisRequest
        {
            PrUrl = "https://github.com/some-org/some-repo/pull/2",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();
        body!.PartiallyEvaluatedFiles.Should().ContainSingle(f => f.Path == "assets/logo.png" && f.Reason == "not_retrievable");
    }

    [Fact]
    public async Task When_every_file_is_fully_retrieved_partiallyEvaluatedFiles_is_empty()
    {
        var fake = new FakeGitHubPullRequestClient(GitHubPullRequestClientResult.SuccessResult(CleanPrSuccess()));
        var client = ClientWithFakeGitHub(fake);

        var response = await client.PostAsJsonAsync(Endpoint, new PrReferenceAnalysisRequest
        {
            PrUrl = "https://github.com/chalk/chalk/pull/688",
        });

        var body = await response.Content.ReadFromJsonAsync<RiskAnalysisResultResponse>();
        body!.PartiallyEvaluatedFiles.Should().BeEmpty();
    }
}
