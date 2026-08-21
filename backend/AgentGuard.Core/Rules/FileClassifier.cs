namespace AgentGuard.Core.Rules;

public enum FileClassification
{
    Source,
    Test,
    Other,
}

/// <summary>
/// Classifies a changed file path as Source, Test, or Other using simple, configurable
/// naming/path conventions — deliberately no semantic understanding of test coverage (FR-004).
/// </summary>
public static class FileClassifier
{
    private static readonly string[] TestPathSegments = ["test", "tests", "__tests__", "spec"];
    private static readonly string[] TestFileInfixes = [".test.", ".spec."];
    private static readonly string[] TestFileSuffixes = ["test.cs", "tests.cs"];

    private static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".tsx", ".js", ".jsx", ".py", ".java", ".go", ".rb", ".php",
        ".cpp", ".c", ".h", ".hpp", ".kt", ".swift",
    };

    public static FileClassification Classify(string path)
    {
        if (IsTest(path))
        {
            return FileClassification.Test;
        }

        var extension = GetExtension(path);
        return SourceExtensions.Contains(extension) ? FileClassification.Source : FileClassification.Other;
    }

    private static bool IsTest(string path)
    {
        var normalized = path.Replace('\\', '/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Any(segment => TestPathSegments.Contains(segment.ToLowerInvariant())))
        {
            return true;
        }

        var fileName = segments.Length > 0 ? segments[^1].ToLowerInvariant() : normalized.ToLowerInvariant();

        return TestFileInfixes.Any(infix => fileName.Contains(infix))
            || TestFileSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal));
    }

    private static string GetExtension(string path)
    {
        var lastDot = path.LastIndexOf('.');
        return lastDot >= 0 ? path[lastDot..] : string.Empty;
    }
}
