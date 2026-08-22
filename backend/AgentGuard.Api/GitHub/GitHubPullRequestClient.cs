using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace AgentGuard.Api.GitHub;

/// <summary>
/// Calls GitHub's REST API directly (no SDK — research.md §1). Retrieval-only: everything it
/// returns feeds AgentGuardAnalyzer unchanged, so this class has no analysis logic of its own.
/// </summary>
public sealed class GitHubPullRequestClient(HttpClient httpClient) : IGitHubPullRequestClient
{
    /// <summary>Signals a rate-limited response deep inside the per-file content loop, so it can
    /// abort the whole retrieval instead of silently marking every remaining file as not-retrievable.</summary>
    private sealed class RateLimitedSignal(string? retryAfterHeader) : Exception
    {
        public string? RetryAfterHeader { get; } = retryAfterHeader;
    }

    public async Task<GitHubPullRequestClientResult> GetPullRequestAsync(
        string owner,
        string repository,
        int prNumber,
        string? credential,
        CancellationToken cancellationToken)
    {
        try
        {
            var prResponse = await SendAsync($"/repos/{owner}/{repository}/pulls/{prNumber}", credential, cancellationToken);
            if (prResponse is null)
            {
                return GitHubPullRequestClientResult.NotFoundOrNoAccessResult();
            }

            var pr = await prResponse.Content.ReadFromJsonAsync<PullRequestDto>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("GitHub returned an empty pull request response.");

            var files = new List<GitHubFileDto>();
            string? nextUrl = $"/repos/{owner}/{repository}/pulls/{prNumber}/files?per_page=100";
            while (nextUrl is not null)
            {
                var filesResponse = await SendAsync(nextUrl, credential, cancellationToken);
                if (filesResponse is null)
                {
                    return GitHubPullRequestClientResult.NotFoundOrNoAccessResult();
                }

                var page = await filesResponse.Content.ReadFromJsonAsync<List<GitHubFileDto>>(cancellationToken: cancellationToken) ?? [];
                files.AddRange(page);
                nextUrl = GetNextPageUrl(filesResponse);
            }

            var retrievedFiles = new List<RetrievedFile>();
            foreach (var file in files)
            {
                var oldContent = file.Status == "added"
                    ? null
                    : await GetFileContentAsync(owner, repository, file.PreviousFilename ?? file.Filename, pr.Base.Sha, credential, cancellationToken);
                var newContent = file.Status == "removed"
                    ? null
                    : await GetFileContentAsync(owner, repository, file.Filename, pr.Head.Sha, credential, cancellationToken);

                var fullyEvaluated = (file.Status == "added" || oldContent is not null)
                    && (file.Status == "removed" || newContent is not null);

                retrievedFiles.Add(new RetrievedFile(
                    Path: file.Filename,
                    GitHubStatus: file.Status,
                    OldContent: oldContent,
                    NewContent: newContent,
                    LinesAdded: file.Additions,
                    LinesDeleted: file.Deletions,
                    FullyEvaluated: fullyEvaluated));
            }

            return GitHubPullRequestClientResult.SuccessResult(new GitHubPullRequestSuccess(pr.Title, retrievedFiles));
        }
        catch (RateLimitedSignal signal)
        {
            return GitHubPullRequestClientResult.RateLimitedResult(signal.RetryAfterHeader);
        }
    }

    /// <summary>Fetches one file's content at a ref. Returns null (not-retrievable, FR-009 /
    /// research.md §5) for a 404, a non-base64 encoding, or a decode failure — never throws for
    /// those cases, since a single unreadable file must not fail the whole PR's analysis.</summary>
    private async Task<string?> GetFileContentAsync(
        string owner, string repository, string path, string sha, string? credential, CancellationToken cancellationToken)
    {
        var url = $"/repos/{owner}/{repository}/contents/{Uri.EscapeDataString(path)}?ref={Uri.EscapeDataString(sha)}";
        var response = await SendAsync(url, credential, cancellationToken);
        if (response is null)
        {
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<ContentsDto>(cancellationToken: cancellationToken);
        if (dto?.Encoding != "base64" || dto.Content is null)
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(dto.Content.Replace("\n", string.Empty));
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Sends one authenticated GitHub API request. Returns null for a 404 — the caller decides
    /// what that means: NotFoundOrNoAccess for the PR/files-list calls (FR-010a), or simply
    /// "not retrievable" for a single file's content (FR-009). Throws
    /// <see cref="RateLimitedSignal"/> for a rate limit, caught once at the top of
    /// <see cref="GetPullRequestAsync"/>.
    /// </summary>
    private async Task<HttpResponseMessage?> SendAsync(string relativeUrl, string? credential, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        if (!string.IsNullOrWhiteSpace(credential))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        }

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests || IsPrimaryRateLimitExhausted(response))
        {
            throw new RateLimitedSignal(response.Headers.RetryAfter?.ToString());
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            // A 403 without rate-limit evidence is most often GitHub's secondary (abuse-detection)
            // rate limit, which doesn't always set X-RateLimit-Remaining — see research.md §3.
            throw new RateLimitedSignal(response.Headers.RetryAfter?.ToString());
        }

        response.EnsureSuccessStatusCode();
        return response;
    }

    private static bool IsPrimaryRateLimitExhausted(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.Forbidden
        && response.Headers.TryGetValues("X-RateLimit-Remaining", out var values)
        && values.FirstOrDefault() == "0";

    private static string? GetNextPageUrl(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var linkValues))
        {
            return null;
        }

        var link = linkValues.FirstOrDefault();
        if (link is null)
        {
            return null;
        }

        foreach (var part in link.Split(','))
        {
            var segments = part.Split(';');
            if (segments.Length < 2)
            {
                continue;
            }

            var relSegment = segments[1].Trim();
            if (relSegment != "rel=\"next\"")
            {
                continue;
            }

            var urlSegment = segments[0].Trim();
            return urlSegment.TrimStart('<').TrimEnd('>');
        }

        return null;
    }

    private sealed record PullRequestDto(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("base")] PullRequestRefDto Base,
        [property: JsonPropertyName("head")] PullRequestRefDto Head);

    private sealed record PullRequestRefDto([property: JsonPropertyName("sha")] string Sha);

    private sealed record GitHubFileDto(
        [property: JsonPropertyName("filename")] string Filename,
        [property: JsonPropertyName("previous_filename")] string? PreviousFilename,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("additions")] int Additions,
        [property: JsonPropertyName("deletions")] int Deletions);

    private sealed record ContentsDto(
        [property: JsonPropertyName("encoding")] string? Encoding,
        [property: JsonPropertyName("content")] string? Content);
}
