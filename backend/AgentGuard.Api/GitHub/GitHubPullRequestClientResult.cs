namespace AgentGuard.Api.GitHub;

public enum GitHubPullRequestClientResultKind
{
    Success,
    NotFoundOrNoAccess,
    RateLimited,
}

/// <summary>One changed file as retrieved from GitHub (data-model.md: Retrieved Change File).</summary>
public sealed record RetrievedFile(
    string Path,
    string GitHubStatus,
    string? OldContent,
    string? NewContent,
    int LinesAdded,
    int LinesDeleted,
    bool FullyEvaluated);

public sealed record GitHubPullRequestSuccess(string PrTitle, IReadOnlyList<RetrievedFile> Files);

/// <summary>
/// Outcome of one GitHub PR retrieval attempt. A flat record with a Kind discriminator, matching
/// this codebase's existing style (plain records, no OOP hierarchies) rather than an abstract
/// class per case.
/// </summary>
public sealed record GitHubPullRequestClientResult
{
    public required GitHubPullRequestClientResultKind Kind { get; init; }
    public GitHubPullRequestSuccess? Success { get; init; }
    public string? RetryAfterHeader { get; init; }

    public static GitHubPullRequestClientResult SuccessResult(GitHubPullRequestSuccess success) =>
        new() { Kind = GitHubPullRequestClientResultKind.Success, Success = success };

    public static GitHubPullRequestClientResult NotFoundOrNoAccessResult() =>
        new() { Kind = GitHubPullRequestClientResultKind.NotFoundOrNoAccess };

    public static GitHubPullRequestClientResult RateLimitedResult(string? retryAfterHeader = null) =>
        new() { Kind = GitHubPullRequestClientResultKind.RateLimited, RetryAfterHeader = retryAfterHeader };
}
