namespace AgentGuard.Api.Contracts;

public sealed record PrReferenceAnalysisRequest
{
    public string? PrUrl { get; init; }
    public string? Owner { get; init; }
    public string? Repository { get; init; }
    public int? PrNumber { get; init; }
    public string? Credential { get; init; }
}
