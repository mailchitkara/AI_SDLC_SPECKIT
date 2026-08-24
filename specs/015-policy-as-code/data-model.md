# Phase 1 Data Model: Policy-as-Code Configuration Loading

No changes to any `AgentGuard.Core` entity — `ForbiddenDependency`/`ForbiddenDependencyConfig` and `BusinessCriticalPath`/`BusinessCriticalPathConfig` already exist exactly as needed (research.md §4).

## Policy file JSON shape (new, AgentGuard.Api-only concept)

```json
{
  "forbiddenDependencies": [
    { "from": "src/Ui/", "to": "MyApp.Data.*" }
  ],
  "businessCriticalPaths": [
    { "pathPattern": "payments/*", "label": "Payment Processing" }
  ]
}
```

Both top-level arrays are optional; an absent key is equivalent to an empty array.

## PolicyFileLoader (new, AgentGuard.Api)

```csharp
namespace AgentGuard.Api.Configuration;

public sealed record LoadedPolicy(ForbiddenDependencyConfig ForbiddenDependencies, BusinessCriticalPathConfig BusinessCriticalPaths);

public static class PolicyFileLoader
{
    // filePath is typically Environment.GetEnvironmentVariable("AGENTGUARD_POLICY_FILE_PATH").
    // Returns both configs as .Empty when filePath is null/empty or the file doesn't exist (FR-002, FR-003).
    // Throws with a clear message when the file exists but fails to parse (FR-004).
    public static LoadedPolicy Load(string? filePath);
}
```

## Program.cs wiring (changed)

```csharp
var loadedPolicy = PolicyFileLoader.Load(Environment.GetEnvironmentVariable("AGENTGUARD_POLICY_FILE_PATH"));
builder.Services.AddSingleton(loadedPolicy.ForbiddenDependencies);
builder.Services.AddSingleton(loadedPolicy.BusinessCriticalPaths);
builder.Services.AddSingleton<AgentGuardAnalyzer>();
```

Replaces the current hardcoded `builder.Services.AddSingleton(ForbiddenDependencyConfig.Empty);` and adds the previously-missing `BusinessCriticalPathConfig` registration — both now flow from the same loader call.

## State / lifecycle note

Read once at process startup. No re-reading, no file watching, no persistence beyond the process's own singleton lifetime — a config change requires a service restart, matching every other environment-variable-driven setting this deployment already has (`FRONTEND_ORIGIN`, `PORT`).
