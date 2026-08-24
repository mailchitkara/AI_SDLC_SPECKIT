using AgentGuard.Core;

namespace AgentGuard.Api.Contracts;

public sealed record ChangedFileRequest
{
    public string? Path { get; init; }
    public string? ChangeType { get; init; }
    public string? OldContent { get; init; }
    public string? NewContent { get; init; }
    public int? LinesAdded { get; init; }
    public int? LinesDeleted { get; init; }
}

public sealed record PullRequestChangeSetRequest
{
    public string? RepositoryName { get; init; }
    public int? PrNumber { get; init; }
    public string? PrTitle { get; init; }
    public List<ChangedFileRequest>? ChangedFiles { get; init; }
    public ThresholdConfigurationRequest? Thresholds { get; init; }
    public List<VulnerableDependencyRequest>? VulnerableDependencies { get; init; }
}

public static class PullRequestChangeSetRequestMapping
{
    /// <summary>Converts an already-validated request into a Core PullRequestChangeSet.</summary>
    public static PullRequestChangeSet ToChangeSet(this PullRequestChangeSetRequest request)
    {
        var changedFiles = (request.ChangedFiles ?? [])
            .Select(f =>
            {
                EnumMappings.TryParseChangeType(f.ChangeType, out var changeType);
                return new ChangedFile(
                    Path: f.Path!,
                    ChangeType: changeType,
                    OldContent: f.OldContent,
                    NewContent: f.NewContent,
                    LinesAdded: f.LinesAdded!.Value,
                    LinesDeleted: f.LinesDeleted!.Value);
            })
            .ToList();

        return new PullRequestChangeSet(
            RepositoryName: request.RepositoryName!,
            PrNumber: request.PrNumber!.Value,
            PrTitle: request.PrTitle!,
            ChangedFiles: changedFiles);
    }
}
