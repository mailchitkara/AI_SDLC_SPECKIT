namespace AgentGuard.Api.Contracts;

/// <summary>data-model.md: Import Error. errorType is one of invalid_reference | not_found_or_no_access | rate_limited.</summary>
public sealed record ImportErrorResponse(string ErrorType, string Message, bool RetryableWithCredential);
