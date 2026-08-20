import type { CheckResult, RiskAnalysisResult } from '../../src/types/riskAnalysis'

const ALL_PASSED_CHECKS: CheckResult[] = [
  { ruleId: 'LARGE_CHANGE_SIZE', ruleName: 'Large Change Size', passed: true },
  { ruleId: 'MISSING_RELATED_TESTS', ruleName: 'Missing Related Tests', passed: true },
  { ruleId: 'API_CONTRACT_BREAKING_CHANGE', ruleName: 'API Contract Breaking Change', passed: true },
  { ruleId: 'ARCHITECTURE_VIOLATION', ruleName: 'Architecture / Dependency Violation', passed: true },
  { ruleId: 'SECRET_DETECTED', ruleName: 'Potential Secret Detected', passed: true },
]

export const cleanResult: RiskAnalysisResult = {
  repositoryName: 'agentguard-demo',
  prNumber: 1,
  prTitle: 'Update README',
  score: 0,
  classification: 'LOW',
  recommendation: 'SAFE_TO_REVIEW',
  checks: ALL_PASSED_CHECKS,
  findings: [],
}

export const blockerResult: RiskAnalysisResult = {
  repositoryName: 'agentguard-demo',
  prNumber: 7,
  prTitle: 'Add logging for debugging',
  score: 100,
  classification: 'CRITICAL',
  recommendation: 'BLOCK_MERGE',
  checks: ALL_PASSED_CHECKS.map((check) =>
    check.ruleId === 'SECRET_DETECTED' ? { ...check, passed: false } : check,
  ),
  findings: [
    {
      ruleId: 'SECRET_DETECTED',
      ruleName: 'Potential Secret Detected',
      severity: 'BLOCKER',
      explanation: 'Changed content matches a recognized AWS access key pattern.',
      evidence: 'AKIA****************',
      location: 'src/config/aws.ts',
      remediation: 'Remove the secret from source control and rotate the credential immediately.',
    },
  ],
}

export const multiRuleResult: RiskAnalysisResult = {
  repositoryName: 'agentguard-demo',
  prNumber: 42,
  prTitle: 'Add customer preferences API',
  score: 45,
  classification: 'MEDIUM',
  recommendation: 'REVIEW_RECOMMENDED',
  checks: ALL_PASSED_CHECKS.map((check) =>
    check.ruleId === 'LARGE_CHANGE_SIZE' || check.ruleId === 'API_CONTRACT_BREAKING_CHANGE'
      ? { ...check, passed: false }
      : check,
  ),
  findings: [
    {
      ruleId: 'LARGE_CHANGE_SIZE',
      ruleName: 'Large Change Size',
      severity: 'LOW',
      explanation: 'This PR changes more than 500 lines or more than 20 files.',
      evidence: '642 lines changed across 27 files',
      location: null,
      remediation: 'Consider splitting this PR into smaller, focused changes.',
    },
    {
      ruleId: 'API_CONTRACT_BREAKING_CHANGE',
      ruleName: 'API Contract Breaking Change',
      severity: 'HIGH',
      explanation: 'A response property was removed from an existing endpoint.',
      evidence: 'Property "email" removed from GET /customers/{id} response',
      location: 'contracts/customer.yaml',
      remediation: 'Restore the property or introduce a new API version.',
    },
  ],
}
