using System.Text.Json;
using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Core.Rules;

/// <summary>
/// FR-005: inspects changed OpenAPI/Swagger JSON contract files and flags exactly four
/// conditions — endpoint removed, HTTP method removed, response property removed, or an
/// optional request property becoming required. No other diff is flagged as breaking.
///
/// V1 scope (research.md §4): only JSON contract files are recognized, parsed with the
/// built-in System.Text.Json — no external OpenAPI/YAML library, keeping the feature
/// dependency-free per plan.md.
/// </summary>
public static class ApiContractBreakingChangeRule
{
    private static readonly string[] HttpMethods =
        ["get", "put", "post", "delete", "options", "head", "patch", "trace"];

    public static IReadOnlyList<Finding> Evaluate(PullRequestChangeSet changeSet)
    {
        var findings = new List<Finding>();

        foreach (var file in changeSet.ChangedFiles)
        {
            if (!IsContractFile(file.Path) || file.ChangeType != ChangeType.Modified)
            {
                continue;
            }

            if (file.OldContent is null || file.NewContent is null)
            {
                continue;
            }

            if (!TryParse(file.OldContent, out var oldDoc) || !TryParse(file.NewContent, out var newDoc))
            {
                continue;
            }

            using (oldDoc)
            using (newDoc)
            {
                findings.AddRange(DiffContract(file.Path, oldDoc.RootElement, newDoc.RootElement));
            }
        }

        return findings;
    }

    private static bool IsContractFile(string path)
    {
        var fileName = path.Replace('\\', '/').Split('/').Last().ToLowerInvariant();
        return fileName.EndsWith(".json", StringComparison.Ordinal)
            && (fileName.Contains("openapi") || fileName.Contains("swagger"));
    }

    private static bool TryParse(string content, out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(content);
            return true;
        }
        catch (JsonException)
        {
            document = null!;
            return false;
        }
    }

    private static IEnumerable<Finding> DiffContract(string filePath, JsonElement oldRoot, JsonElement newRoot)
    {
        if (!TryGetProperty(oldRoot, "paths", out var oldPaths) || oldPaths.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        TryGetProperty(newRoot, "paths", out var newPaths);
        var newPathsHasValue = newPaths.ValueKind == JsonValueKind.Object;

        foreach (var oldPathProperty in oldPaths.EnumerateObject())
        {
            var endpoint = oldPathProperty.Name;

            if (!newPathsHasValue || !newPaths.TryGetProperty(endpoint, out var newPathItem))
            {
                yield return BreakingChangeFinding(
                    filePath,
                    $"Endpoint '{endpoint}' was removed.",
                    $"Endpoint removed: {endpoint}");
                continue;
            }

            var oldPathItem = oldPathProperty.Value;

            foreach (var method in HttpMethods)
            {
                if (!oldPathItem.TryGetProperty(method, out var oldOperation))
                {
                    continue;
                }

                if (!newPathItem.TryGetProperty(method, out var newOperation))
                {
                    yield return BreakingChangeFinding(
                        filePath,
                        $"HTTP method '{method.ToUpperInvariant()}' was removed from endpoint '{endpoint}'.",
                        $"Method removed: {method.ToUpperInvariant()} {endpoint}");
                    continue;
                }

                foreach (var removedProperty in RemovedResponseProperties(oldOperation, newOperation))
                {
                    yield return BreakingChangeFinding(
                        filePath,
                        $"Response property '{removedProperty}' was removed from '{method.ToUpperInvariant()} {endpoint}'.",
                        $"Property '{removedProperty}' removed from {method.ToUpperInvariant()} {endpoint} response");
                }

                foreach (var newlyRequiredProperty in RequestPropertiesBecomeRequired(oldOperation, newOperation))
                {
                    yield return BreakingChangeFinding(
                        filePath,
                        $"Request property '{newlyRequiredProperty}' changed from optional to required on " +
                        $"'{method.ToUpperInvariant()} {endpoint}'.",
                        $"Property '{newlyRequiredProperty}' became required on {method.ToUpperInvariant()} {endpoint} request");
                }
            }
        }
    }

    private static Finding BreakingChangeFinding(string filePath, string explanation, string evidence) => new(
        RuleId: RuleCatalog.ApiContractBreakingChange.Id,
        RuleName: RuleCatalog.ApiContractBreakingChange.Name,
        Severity: RuleCatalog.ApiContractBreakingChange.DefaultSeverity,
        Explanation: explanation,
        Evidence: evidence,
        Location: filePath,
        Remediation: "Restore the removed capability or introduce a new, explicitly versioned API instead.",
        Dimension: RuleCatalog.ApiContractBreakingChange.DefaultDimension,
        Confidence: Confidence.Certain,
        Kind: FindingKind.Deterministic);

    private static IEnumerable<string> RemovedResponseProperties(JsonElement oldOperation, JsonElement newOperation)
    {
        var oldProperties = ResponseSchemaProperties(oldOperation);
        var newProperties = ResponseSchemaProperties(newOperation);

        return oldProperties.Where(p => !newProperties.Contains(p));
    }

    private static HashSet<string> ResponseSchemaProperties(JsonElement operation)
    {
        var properties = new HashSet<string>(StringComparer.Ordinal);

        if (!TryGetProperty(operation, "responses", out var responses) || responses.ValueKind != JsonValueKind.Object)
        {
            return properties;
        }

        foreach (var response in responses.EnumerateObject())
        {
            if (!TryGetProperty(response.Value, "content", out var content) || content.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var mediaType in content.EnumerateObject())
            {
                if (TryGetProperty(mediaType.Value, "schema", out var schema)
                    && TryGetProperty(schema, "properties", out var schemaProperties)
                    && schemaProperties.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in schemaProperties.EnumerateObject())
                    {
                        properties.Add(property.Name);
                    }
                }
            }
        }

        return properties;
    }

    private static IEnumerable<string> RequestPropertiesBecomeRequired(JsonElement oldOperation, JsonElement newOperation)
    {
        var oldOptional = RequestOptionalProperties(oldOperation);
        var newRequired = RequestRequiredProperties(newOperation);

        return oldOptional.Where(newRequired.Contains);
    }

    private static HashSet<string> RequestOptionalProperties(JsonElement operation)
    {
        var (allProperties, required) = RequestSchemaPropertiesAndRequired(operation);
        allProperties.ExceptWith(required);
        return allProperties;
    }

    private static HashSet<string> RequestRequiredProperties(JsonElement operation) =>
        RequestSchemaPropertiesAndRequired(operation).Required;

    private static (HashSet<string> AllProperties, HashSet<string> Required) RequestSchemaPropertiesAndRequired(
        JsonElement operation)
    {
        var allProperties = new HashSet<string>(StringComparer.Ordinal);
        var required = new HashSet<string>(StringComparer.Ordinal);

        if (!TryGetProperty(operation, "requestBody", out var requestBody)
            || !TryGetProperty(requestBody, "content", out var content)
            || content.ValueKind != JsonValueKind.Object)
        {
            return (allProperties, required);
        }

        foreach (var mediaType in content.EnumerateObject())
        {
            if (!TryGetProperty(mediaType.Value, "schema", out var schema))
            {
                continue;
            }

            if (TryGetProperty(schema, "properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in properties.EnumerateObject())
                {
                    allProperties.Add(property.Name);
                }
            }

            if (TryGetProperty(schema, "required", out var requiredArray) && requiredArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in requiredArray.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        required.Add(item.GetString()!);
                    }
                }
            }
        }

        return (allProperties, required);
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}
