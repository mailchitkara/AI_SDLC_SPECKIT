using AgentGuard.Core;

namespace AgentGuard.Core.Tests;

internal static class TestChangeSets
{
    public static PullRequestChangeSet WithFiles(params ChangedFile[] files) =>
        new("agentguard-demo", 1, "Test PR", files);

    public static ChangedFile Added(string path, string newContent, int linesAdded = 1) =>
        new(path, ChangeType.Added, null, newContent, linesAdded, 0);

    public static ChangedFile Modified(
        string path, string? oldContent, string? newContent, int linesAdded = 1, int linesDeleted = 1) =>
        new(path, ChangeType.Modified, oldContent, newContent, linesAdded, linesDeleted);

    public static ChangedFile Deleted(string path, string oldContent, int linesDeleted = 1) =>
        new(path, ChangeType.Deleted, oldContent, null, 0, linesDeleted);
}
