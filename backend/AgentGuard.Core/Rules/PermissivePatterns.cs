using System.Text.RegularExpressions;

namespace AgentGuard.Core.Rules;

public sealed record PermissivePattern(string Name, Regex Pattern, string RemediationHint);

/// <summary>
/// Fixed set of overly-permissive access-control patterns (006-security-risk-rules FR-001,
/// research.md §1). Deliberately narrow and text-pattern-based, mirroring SecretPatterns' shape,
/// rather than a general static-analysis engine (FR-008).
/// </summary>
public static partial class PermissivePatterns
{
    public static readonly IReadOnlyList<PermissivePattern> All =
    [
        new(
            "Wildcard CORS Origin (ASP.NET Core)",
            WildcardCorsAspNetPattern(),
            "Scope the CORS policy to a specific, known set of origins instead of AllowAnyOrigin()."),
        new(
            "Wildcard CORS Origin (Express/Node cors package)",
            WildcardCorsExpressPattern(),
            "Set 'origin' to a specific allow-list of origins instead of '*'."),
        new(
            "Wildcard CORS Origin (raw header)",
            WildcardCorsHeaderPattern(),
            "Set the Access-Control-Allow-Origin header to a specific origin, or derive it from a validated allow-list, instead of '*'."),
        new(
            "Disabled Authorization (AllowAnonymous attribute)",
            DisabledAuthorizationPattern(),
            "Remove the AllowAnonymous attribute and restore the intended authorization requirement, or confirm this endpoint is genuinely meant to be public."),
        new(
            "Wildcard Allowed Hosts (Django-style)",
            WildcardAllowedHostsPattern(),
            "Set ALLOWED_HOSTS to the specific set of hostnames this deployment actually serves, instead of '*'."),
    ];

    [GeneratedRegex(@"\.AllowAnyOrigin\s*\(\s*\)")]
    private static partial Regex WildcardCorsAspNetPattern();

    [GeneratedRegex(@"\borigin\s*:\s*['""]\*['""]")]
    private static partial Regex WildcardCorsExpressPattern();

    // [:=] covers header/config literal syntax (e.g. the CORS response header set to a wildcard
    // origin via a colon or equals) and object-key assignment (headers['...'] = '*'); [,] covers
    // a setHeader-style call taking the header name and value as separate arguments.
    [GeneratedRegex(@"Access-Control-Allow-Origin['""]?\s*[:,=]\s*['""]\*['""]")]
    private static partial Regex WildcardCorsHeaderPattern();

    [GeneratedRegex(@"\[AllowAnonymous\]")]
    private static partial Regex DisabledAuthorizationPattern();

    [GeneratedRegex(@"ALLOWED_HOSTS\s*=\s*\[\s*['""]\*['""]\s*\]")]
    private static partial Regex WildcardAllowedHostsPattern();
}
