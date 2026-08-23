using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Api.Contracts;

/// <summary>FR-007: optional, per-request classification score bands. Never persisted.</summary>
public sealed record ThresholdConfigurationRequest
{
    public int? LowMax { get; init; }
    public int? MediumMax { get; init; }
    public int? HighMax { get; init; }
}

public static class ThresholdConfigurationRequestValidator
{
    /// <summary>FR-008: all three fields required together; must satisfy 0 &lt;= LowMax &lt; MediumMax &lt; HighMax &lt; 100.</summary>
    public static IReadOnlyList<string> Validate(ThresholdConfigurationRequest? request)
    {
        var errors = new List<string>();

        if (request is null)
        {
            return errors;
        }

        if (request.LowMax is null || request.MediumMax is null || request.HighMax is null)
        {
            errors.Add("thresholds must include lowMax, mediumMax, and highMax together, or be omitted entirely.");
            return errors;
        }

        var (lowMax, mediumMax, highMax) = (request.LowMax.Value, request.MediumMax.Value, request.HighMax.Value);

        if (lowMax < 0 || lowMax >= mediumMax || mediumMax >= highMax || highMax >= 100)
        {
            errors.Add("thresholds must satisfy 0 <= lowMax < mediumMax < highMax < 100.");
        }

        return errors;
    }

    public static ThresholdConfiguration ToThresholdConfiguration(this ThresholdConfigurationRequest request) =>
        new(request.LowMax!.Value, request.MediumMax!.Value, request.HighMax!.Value);
}
