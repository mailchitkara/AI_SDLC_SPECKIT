using AgentGuard.Core.Dependencies;
using AgentGuard.Core.Findings;
using AgentGuard.Core.PolicyEngine;
using AgentGuard.Core.Rules;

namespace AgentGuard.Core;

/// <summary>
/// Runs all five fixed V1 rules against a PullRequestChangeSet and produces the full,
/// deterministic RiskAnalysisResult (FR-002, FR-011, FR-013).
/// </summary>
public sealed class AgentGuardAnalyzer
{
    private readonly ForbiddenDependencyConfig _forbiddenDependencyConfig;
    private readonly PolicyEngine.BusinessCriticalPathConfig _businessCriticalPathConfig;
    private readonly RiskEngine.RiskGovernancePolicy _riskGovernancePolicy;

    public AgentGuardAnalyzer(
        ForbiddenDependencyConfig? forbiddenDependencyConfig = null,
        PolicyEngine.BusinessCriticalPathConfig? businessCriticalPathConfig = null,
        RiskEngine.RiskGovernancePolicy? riskGovernancePolicy = null)
    {
        _forbiddenDependencyConfig = forbiddenDependencyConfig ?? ForbiddenDependencyConfig.Empty;
        _businessCriticalPathConfig = businessCriticalPathConfig ?? PolicyEngine.BusinessCriticalPathConfig.Empty;
        _riskGovernancePolicy = riskGovernancePolicy ?? RiskEngine.RiskGovernancePolicy.Empty;
    }

    public RiskEngine.RiskAnalysisResult Analyze(
        PullRequestChangeSet changeSet,
        RiskEngine.ThresholdConfiguration? thresholds = null,
        IReadOnlyList<VulnerableDependency>? vulnerableDependencies = null)
    {
        var findingsByRule = new (Rule Rule, IReadOnlyList<Finding> Findings)[]
        {
            (RuleCatalog.LargeChangeSize, LargeChangeSizeRule.Evaluate(changeSet)),
            (RuleCatalog.MissingRelatedTests, MissingRelatedTestsRule.Evaluate(changeSet)),
            (RuleCatalog.ApiContractBreakingChange, ApiContractBreakingChangeRule.Evaluate(changeSet)),
            (RuleCatalog.ArchitectureViolation, ArchitectureViolationRule.Evaluate(changeSet, _forbiddenDependencyConfig)),
            (RuleCatalog.SecretDetected, SecretDetectedRule.Evaluate(changeSet)),
            (RuleCatalog.OverlyPermissiveAccess, OverlyPermissiveAccessRule.Evaluate(changeSet)),
            (RuleCatalog.DisabledTest, DisabledTestRule.Evaluate(changeSet)),
            (RuleCatalog.SwallowedException, SwallowedExceptionRule.Evaluate(changeSet)),
            (RuleCatalog.GeneratedFileModified, GeneratedFileModifiedRule.Evaluate(changeSet)),
            (RuleCatalog.TodoStub, TodoStubRule.Evaluate(changeSet)),
            (RuleCatalog.InsecureConfiguration, InsecureConfigurationRule.Evaluate(changeSet)),
            (RuleCatalog.VulnerableDependency, VulnerableDependencyRule.Evaluate(vulnerableDependencies ?? [])),
            (RuleCatalog.BusinessCriticalPath, BusinessCriticalPathRule.Evaluate(changeSet, _businessCriticalPathConfig)),
            (RuleCatalog.LargeNewFile, LargeNewFileRule.Evaluate(changeSet)),
        };

        var allFindings = FindingOrdering.Stable(findingsByRule.SelectMany(rf => rf.Findings));

        var checks = RuleCatalog.All
            .Select(rule =>
            {
                var findingCount = findingsByRule.First(rf => rf.Rule.Id == rule.Id).Findings.Count;
                return new CheckResult(rule.Id, rule.Name, Passed: findingCount == 0);
            })
            .ToList();

        var scored = RiskEngine.RiskEngine.Evaluate(allFindings, thresholds, _riskGovernancePolicy);

        return new RiskEngine.RiskAnalysisResult(
            RepositoryName: changeSet.RepositoryName,
            PrNumber: changeSet.PrNumber,
            PrTitle: changeSet.PrTitle,
            Score: scored.Score,
            Classification: scored.Classification,
            Recommendation: scored.Recommendation,
            RecommendationForcedByOverride: scored.RecommendationForcedByOverride,
            RecommendationForcedByGovernancePolicy: scored.RecommendationForcedByGovernancePolicy,
            Checks: checks,
            Findings: allFindings);
    }
}
