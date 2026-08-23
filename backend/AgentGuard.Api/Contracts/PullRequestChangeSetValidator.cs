namespace AgentGuard.Api.Contracts;

/// <summary>FR-002: validates a request before analysis; an empty changedFiles array is valid input.</summary>
public static class PullRequestChangeSetValidator
{
    public static IReadOnlyList<string> Validate(PullRequestChangeSetRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.RepositoryName))
        {
            errors.Add("repositoryName is required.");
        }

        if (request.PrNumber is null or <= 0)
        {
            errors.Add("prNumber is required and must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(request.PrTitle))
        {
            errors.Add("prTitle is required.");
        }

        if (request.ChangedFiles is null)
        {
            errors.Add("changedFiles is required (an empty array is allowed).");
        }
        else
        {
            for (var i = 0; i < request.ChangedFiles.Count; i++)
            {
                ValidateChangedFile(request.ChangedFiles[i], i, errors);
            }
        }

        errors.AddRange(ThresholdConfigurationRequestValidator.Validate(request.Thresholds));

        return errors;
    }

    private static void ValidateChangedFile(ChangedFileRequest file, int index, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(file.Path))
        {
            errors.Add($"changedFiles[{index}].path is required.");
        }

        if (!EnumMappings.TryParseChangeType(file.ChangeType, out _))
        {
            errors.Add($"changedFiles[{index}].changeType must be one of ADDED, MODIFIED, DELETED, RENAMED.");
        }

        if (file.LinesAdded is null or < 0)
        {
            errors.Add($"changedFiles[{index}].linesAdded is required and must be >= 0.");
        }

        if (file.LinesDeleted is null or < 0)
        {
            errors.Add($"changedFiles[{index}].linesDeleted is required and must be >= 0.");
        }
    }
}
