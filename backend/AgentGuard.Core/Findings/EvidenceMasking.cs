namespace AgentGuard.Core.Findings;

/// <summary>
/// Masks a raw secret value so the complete secret never exists in any Finding, API response,
/// UI output, or log (FR-010). Masking happens at the point of Finding construction — callers
/// MUST NOT hold onto or log the raw value after calling this.
/// </summary>
public static class EvidenceMasking
{
    private const int VisibleEdgeLength = 4;

    public static string Mask(string rawSecret)
    {
        if (rawSecret.Length <= VisibleEdgeLength * 2)
        {
            return new string('*', rawSecret.Length);
        }

        var prefix = rawSecret[..VisibleEdgeLength];
        var suffix = rawSecret[^VisibleEdgeLength..];
        var maskedLength = rawSecret.Length - (VisibleEdgeLength * 2);

        return prefix + new string('*', maskedLength) + suffix;
    }
}
