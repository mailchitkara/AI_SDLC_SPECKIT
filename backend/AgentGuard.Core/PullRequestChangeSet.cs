namespace AgentGuard.Core;

public enum ChangeType
{
    Added,
    Modified,
    Deleted,
    Renamed,
}

public sealed record ChangedFile(
    string Path,
    ChangeType ChangeType,
    string? OldContent,
    string? NewContent,
    int LinesAdded,
    int LinesDeleted);

public sealed record PullRequestChangeSet(
    string RepositoryName,
    int PrNumber,
    string PrTitle,
    IReadOnlyList<ChangedFile> ChangedFiles);
