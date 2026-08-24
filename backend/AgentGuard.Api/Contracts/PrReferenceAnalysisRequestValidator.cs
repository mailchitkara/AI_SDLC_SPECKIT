using System.Text.RegularExpressions;

namespace AgentGuard.Api.Contracts;

/// <summary>FR-001, FR-010: validates a PR reference before any GitHub call is made.</summary>
public static partial class PrReferenceAnalysisRequestValidator
{
    [GeneratedRegex(@"^https?://github\.com/([^/]+)/([^/]+)/pull/(\d+)")]
    private static partial Regex PrUrlPattern();

    public static IReadOnlyList<string> Validate(PrReferenceAnalysisRequest request)
    {
        var errors = new List<string>();

        var hasPrUrl = !string.IsNullOrWhiteSpace(request.PrUrl);
        var hasTrio = !string.IsNullOrWhiteSpace(request.Owner)
            && !string.IsNullOrWhiteSpace(request.Repository)
            && request.PrNumber is > 0;
        var hasAnyTrioField = !string.IsNullOrWhiteSpace(request.Owner)
            || !string.IsNullOrWhiteSpace(request.Repository)
            || request.PrNumber is not null;

        if (hasPrUrl && hasAnyTrioField)
        {
            errors.Add("Provide either prUrl or the owner/repository/prNumber trio, not both.");
            return errors;
        }

        if (!hasPrUrl && !hasTrio)
        {
            errors.Add(hasAnyTrioField
                ? "owner, repository, and prNumber must all be provided together."
                : "Provide either prUrl or the owner/repository/prNumber trio.");
            return errors;
        }

        if (hasPrUrl && !PrUrlPattern().IsMatch(request.PrUrl!.Trim()))
        {
            errors.Add("prUrl must be a valid GitHub pull request URL, e.g. https://github.com/{owner}/{repo}/pull/{number}.");
        }

        errors.AddRange(ThresholdConfigurationRequestValidator.Validate(request.Thresholds));
        errors.AddRange(VulnerableDependencyRequestValidator.Validate(request.VulnerableDependencies));

        return errors;
    }

    /// <summary>Resolves owner/repository/prNumber from an already-validated request.</summary>
    public static (string Owner, string Repository, int PrNumber) Resolve(this PrReferenceAnalysisRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.PrUrl))
        {
            var match = PrUrlPattern().Match(request.PrUrl.Trim());
            return (match.Groups[1].Value, match.Groups[2].Value, int.Parse(match.Groups[3].Value));
        }

        return (request.Owner!, request.Repository!, request.PrNumber!.Value);
    }
}
