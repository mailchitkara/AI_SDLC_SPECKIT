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

// 005-risk-engine-foundation: optional (not required) so existing fixtures/tests built against
// the pre-feature shape keep compiling — the API always sends these, but the type doesn't force it.
export type RiskDimension =
  | 'SECURITY'
  | 'TESTING'
  | 'COMPATIBILITY'
  | 'ARCHITECTURE'
  | 'CHANGE_MANAGEMENT'
  | 'DEPENDENCIES'
  | 'RELIABILITY'
  | 'CONFIGURATION'

export type Confidence = 'CERTAIN' | 'HIGH' | 'MEDIUM' | 'LOW'

export type FindingKind = 'DETERMINISTIC' | 'CONTEXTUAL'

export interface Finding {
  ruleId: RuleId
  ruleName: string
  severity: Severity
  explanation: string
  evidence: string
  location?: string | null
  remediation: string
  dimension?: RiskDimension
  confidence?: Confidence
  kind?: FindingKind
  mandatoryOverride?: boolean
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

export interface PartiallyEvaluatedFile {
  path: string
  reason: string
}

export interface RiskAnalysisResult {
  repositoryName: string
  prNumber: number
  prTitle: string
  score: number
  classification: RiskClassification
  recommendation: Recommendation
  recommendationForcedByOverride?: boolean
  checks: CheckResult[]
  findings: Finding[]
  partiallyEvaluatedFiles?: PartiallyEvaluatedFile[]
}

export interface ValidationError {
  message: string
  errors: string[]
}

// Mirrors specs/003-github-pr-import/contracts/pr-reference-analysis-endpoint.md

export interface PrReferenceRequest {
  prUrl?: string
  owner?: string
  repository?: string
  prNumber?: number
  credential?: string
}

export type ImportErrorType = 'invalid_reference' | 'not_found_or_no_access' | 'rate_limited'

export interface ImportError {
  errorType: ImportErrorType
  message: string
  retryableWithCredential: boolean
}
