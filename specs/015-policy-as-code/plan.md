# Implementation Plan: Policy-as-Code Configuration Loading

**Branch**: `feature/policy-as-code` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/015-policy-as-code/spec.md`

## Summary

Add a JSON policy-file loader in `AgentGuard.Api` that populates `ForbiddenDependencyConfig` and `BusinessCriticalPathConfig` (both already-shipped `AgentGuard.Core` types) at service startup, from a file path supplied via an environment variable. No change to either rule's evaluation logic — this only fixes how their existing configuration seams get populated, closing a gap where both have been unreachable since they shipped.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: `System.Text.Json` (already part of the .NET SDK, already used implicitly by ASP.NET Core's request binding — no new package).

**Storage**: N/A — the policy file is read once at startup, not persisted or re-read; matches every other config's stateless, in-memory-singleton shape.

**Testing**: xUnit. New tests target the loader function directly (given a file path/content, assert the resulting configs), not the DI wiring itself (matching how other `Program.cs`-level wiring in this codebase isn't unit-tested — the wiring is a thin, five-line composition root).

**Target Platform**: Same deployed `agentguard-api` service — this changes only how it starts, not its runtime request-handling shape.

**Project Type**: Extension of `AgentGuard.Api` only — the loader parses JSON into existing `AgentGuard.Core.PolicyEngine` types, so `AgentGuard.Core` itself doesn't need `System.Text.Json` as a dependency (keeping JSON/wire-format concerns in the API layer, per the constitution's separation-of-concerns principle). No `AgentGuard.Core` or `frontend` changes.

**Performance Goals**: N/A — startup-time only, not on the request path.

**Constraints**: MUST NOT change any of the fourteen existing rules' behavior when no policy file is configured (FR-002, SC-002, SC-004). MUST fail loudly (not silently) on a malformed-but-present file (FR-004).

**Scale/Scope**: One new loader class/file, one new JSON schema (informally documented, not a full JSON Schema file for this increment), a small `Program.cs` change to call the loader instead of hardcoding `ForbiddenDependencyConfig.Empty` and omitting `BusinessCriticalPathConfig`. No new endpoint, no request/response contract change.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: The loader (JSON parsing, a wire-format concern) lives in `AgentGuard.Api`, producing plain `AgentGuard.Core.PolicyEngine` objects `AgentGuard.Core` already defines — `AgentGuard.Core` gains no new dependency and no new concept. Matches exactly how `VulnerableDependencyRequest`'s Api-layer mapping already works.
- **UI Contract**: No change — this is a backend/operator-facing capability with no new API surface at all, let alone one the frontend would need to render.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged.*

## Project Structure

### Documentation (this feature)

```text
specs/015-policy-as-code/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output
```

No `contracts/` directory — no API request/response shape changes; the "contract" here is a local file format, documented in data-model.md instead.

### Source Code (repository root)

```text
backend/
├── AgentGuard.Api/
│   ├── Configuration/
│   │   └── PolicyFileLoader.cs   # new: Load(string? filePath) -> (ForbiddenDependencyConfig, BusinessCriticalPathConfig)
│   └── Program.cs                # changed: call PolicyFileLoader.Load instead of hardcoding .Empty
└── AgentGuard.Api.Tests/
    └── Configuration/
        └── PolicyFileLoaderTests.cs  # new
```

**Structure Decision**: Entirely new to `AgentGuard.Api` — no existing file's behavior changes except `Program.cs`'s composition root, which gains two lines (a loader call, and a `BusinessCriticalPathConfig` registration that was simply missing before).

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
