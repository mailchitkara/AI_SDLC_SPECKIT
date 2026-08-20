// Mirrors specs/001-pr-risk-analysis-v1/contracts/openapi.yaml

export type ChangeType = 'ADDED' | 'MODIFIED' | 'DELETED' | 'RENAMED'

export interface ChangedFile {
  path: string
  changeType: ChangeType
  oldContent?: string | null
  newContent?: string | null
  linesAdded: number
  linesDeleted: number
}

export interface PullRequestChangeSet {
  repositoryName: string
  prNumber: number
  prTitle: string
  changedFiles: ChangedFile[]
}

export type Severity = 'INFO' | 'LOW' | 'MEDIUM' | 'HIGH' | 'BLOCKER'

export type RuleId =
  | 'LARGE_CHANGE_SIZE'
  | 'MISSING_RELATED_TESTS'
  | 'API_CONTRACT_BREAKING_CHANGE'
  | 'ARCHITECTURE_VIOLATION'
  | 'SECRET_DETECTED'

export interface Finding {
  ruleId: RuleId
  ruleName: string
  severity: Severity
  explanation: string
  evidence: string
  location?: string | null
  remediation: string
}

export interface CheckResult {
  ruleId: RuleId
  ruleName: string
  passed: boolean
}

export type RiskClassification = 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL'

export type Recommendation =
  | 'SAFE_TO_REVIEW'
  | 'REVIEW_RECOMMENDED'
  | 'HUMAN_REVIEW_REQUIRED'
  | 'BLOCK_MERGE'

export interface RiskAnalysisResult {
  repositoryName: string
  prNumber: number
  prTitle: string
  score: number
  classification: RiskClassification
  recommendation: Recommendation
  checks: CheckResult[]
  findings: Finding[]
}

export interface ValidationError {
  message: string
  errors: string[]
}
