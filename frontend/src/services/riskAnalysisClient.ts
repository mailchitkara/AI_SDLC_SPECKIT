import type { PullRequestChangeSet, RiskAnalysisResult, ValidationError } from '../types/riskAnalysis'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

export class RiskAnalysisValidationError extends Error {
  readonly errors: string[]

  constructor(body: ValidationError) {
    super(body.message)
    this.name = 'RiskAnalysisValidationError'
    this.errors = body.errors
  }
}

export async function analyzePullRequest(
  changeSet: PullRequestChangeSet,
): Promise<RiskAnalysisResult> {
  const response = await fetch(`${API_BASE_URL}/api/pr-risk-analysis`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(changeSet),
  })

  if (response.status === 400) {
    const body = (await response.json()) as ValidationError
    throw new RiskAnalysisValidationError(body)
  }

  if (!response.ok) {
    throw new Error(`PR risk analysis request failed with status ${response.status}`)
  }

  return (await response.json()) as RiskAnalysisResult
}
