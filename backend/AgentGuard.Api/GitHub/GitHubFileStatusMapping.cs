using AgentGuard.Core;

namespace AgentGuard.Api.GitHub;

/// <summary>Maps GitHub's pull-request file status strings onto AgentGuard.Core's ChangeType (FR-003).</summary>
public static class GitHubFileStatusMapping
{
    public static bool TryMapChangeType(string? gitHubStatus, out ChangeType changeType)
    {
        switch (gitHubStatus)
        {
            case "added": changeType = ChangeType.Added; return true;
            case "removed": changeType = ChangeType.Deleted; return true;
            case "modified": changeType = ChangeType.Modified; return true;
            case "renamed": changeType = ChangeType.Renamed; return true;
            default: changeType = default; return false;
        }
    }
}
