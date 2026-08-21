using AgentGuard.Api.Contracts;
using AgentGuard.Core;

namespace AgentGuard.Api.Endpoints;

public static class PrRiskAnalysisEndpoint
{
    public static void MapPrRiskAnalysisEndpoint(this WebApplication app)
    {
        app.MapPost("/api/pr-risk-analysis", (PullRequestChangeSetRequest request, AgentGuardAnalyzer analyzer) =>
        {
            var errors = PullRequestChangeSetValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.BadRequest(
                    new ValidationErrorResponse("The submitted change data is invalid.", errors));
            }

            var result = analyzer.Analyze(request.ToChangeSet());
            return Results.Ok(result.ToResponse());
        })
        .WithName("AnalyzePullRequest")
        .Produces<RiskAnalysisResultResponse>(StatusCodes.Status200OK)
        .Produces<ValidationErrorResponse>(StatusCodes.Status400BadRequest);
    }
}
