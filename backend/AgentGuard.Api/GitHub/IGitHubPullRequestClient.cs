namespace AgentGuard.Api.GitHub;

/// <summary>
/// Retrieves a single pull request's metadata and changed files from GitHub. Abstracted so
/// endpoint tests can substitute a fake rather than calling the real GitHub API (research.md §2).
/// </summary>
public interface IGitHubPullRequestClient
{
    Task<GitHubPullRequestClientResult> GetPullRequestAsync(
        string owner,
        string repository,
        int prNumber,
        string? credential,
        CancellationToken cancellationToken);
}
