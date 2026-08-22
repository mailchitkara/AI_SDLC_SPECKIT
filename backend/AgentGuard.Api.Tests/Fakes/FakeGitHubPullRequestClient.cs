using AgentGuard.Api.GitHub;

namespace AgentGuard.Api.Tests.Fakes;

/// <summary>
/// Test double for IGitHubPullRequestClient (research.md §2). Takes a function from the
/// credential passed on each call to the result to return, so a single fake instance can model
/// the credential-retry flow (003 US3: no credential -> NotFoundOrNoAccess, a working credential
/// -> Success) as well as a fixed canned response for simpler cases.
/// </summary>
public sealed class FakeGitHubPullRequestClient : IGitHubPullRequestClient
{
    private readonly Func<string?, GitHubPullRequestClientResult> _resultFor;
    public List<(string Owner, string Repository, int PrNumber, string? Credential)> Calls { get; } = [];

    public FakeGitHubPullRequestClient(GitHubPullRequestClientResult result)
        : this(_ => result)
    {
    }

    public FakeGitHubPullRequestClient(Func<string?, GitHubPullRequestClientResult> resultFor)
    {
        _resultFor = resultFor;
    }

    public Task<GitHubPullRequestClientResult> GetPullRequestAsync(
        string owner,
        string repository,
        int prNumber,
        string? credential,
        CancellationToken cancellationToken)
    {
        Calls.Add((owner, repository, prNumber, credential));
        return Task.FromResult(_resultFor(credential));
    }
}
