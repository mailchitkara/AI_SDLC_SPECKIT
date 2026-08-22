using AgentGuard.Api.Contracts;
using AgentGuard.Api.GitHub;
using AgentGuard.Core;

namespace AgentGuard.Api.Endpoints;

public static class PrReferenceAnalysisEndpoint
{
    public static void MapPrReferenceAnalysisEndpoint(this WebApplication app)
    {
        app.MapPost("/api/pr-risk-analysis/from-reference", async (
            PrReferenceAnalysisRequest request,
            IGitHubPullRequestClient gitHubClient,
            AgentGuardAnalyzer analyzer,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var errors = PrReferenceAnalysisRequestValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.BadRequest(new ImportErrorResponse(
                    ErrorType: "invalid_reference",
                    Message: string.Join(" ", errors),
                    RetryableWithCredential: false));
            }

            var (owner, repository, prNumber) = request.Resolve();
            var result = await gitHubClient.GetPullRequestAsync(owner, repository, prNumber, request.Credential, cancellationToken);

            if (result.Kind == GitHubPullRequestClientResultKind.NotFoundOrNoAccess)
            {
                return Results.Json(
                    new ImportErrorResponse(
                        ErrorType: "not_found_or_no_access",
                        Message: "The PR could not be found, or you may not have access to it. If this is a private repository, retry with a credential that has access.",
                        RetryableWithCredential: true),
                    statusCode: StatusCodes.Status404NotFound);
            }

            if (result.Kind == GitHubPullRequestClientResultKind.RateLimited)
            {
                if (!string.IsNullOrEmpty(result.RetryAfterHeader))
                {
                    httpContext.Response.Headers.RetryAfter = result.RetryAfterHeader;
                }

                return Results.Json(
                    new ImportErrorResponse(
                        ErrorType: "rate_limited",
                        Message: "GitHub is rate-limiting this request. Try again later, or supply a credential to raise the rate limit.",
                        RetryableWithCredential: false),
                    statusCode: StatusCodes.Status429TooManyRequests);
            }

            var success = result.Success!;
            var changedFiles = success.Files
                .Select(f =>
                {
                    GitHubFileStatusMapping.TryMapChangeType(f.GitHubStatus, out var changeType);
                    return new ChangedFile(
                        Path: f.Path,
                        ChangeType: changeType,
                        OldContent: f.OldContent,
                        NewContent: f.NewContent,
                        LinesAdded: f.LinesAdded,
                        LinesDeleted: f.LinesDeleted);
                })
                .ToList();

            var changeSet = new PullRequestChangeSet(
                RepositoryName: repository,
                PrNumber: prNumber,
                PrTitle: success.PrTitle,
                ChangedFiles: changedFiles);

            var analysisResult = analyzer.Analyze(changeSet);

            var partiallyEvaluatedFiles = success.Files
                .Where(f => !f.FullyEvaluated)
                .Select(f => new PartiallyEvaluatedFileResponse(f.Path, "not_retrievable"))
                .ToList();

            return Results.Ok(analysisResult.ToResponse(partiallyEvaluatedFiles));
        })
        .WithName("AnalyzePullRequestFromReference")
        .Produces<RiskAnalysisResultResponse>(StatusCodes.Status200OK)
        .Produces<ImportErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ImportErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ImportErrorResponse>(StatusCodes.Status429TooManyRequests);
    }
}
