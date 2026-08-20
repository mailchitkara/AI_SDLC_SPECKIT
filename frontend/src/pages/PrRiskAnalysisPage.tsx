import { useState } from 'react'
import type { PullRequestChangeSet, RiskAnalysisResult } from '../types/riskAnalysis'
import { analyzePullRequest, RiskAnalysisValidationError } from '../services/riskAnalysisClient'
import { RiskSummary } from '../components/RiskSummary'
import { FindingsList } from '../components/FindingsList'
import { ChecksSummary } from '../components/ChecksSummary'

type Status = 'idle' | 'loading' | 'error' | 'success'

const SAMPLE_CHANGE_SET: PullRequestChangeSet = {
  repositoryName: 'agentguard-demo',
  prNumber: 42,
  prTitle: 'Add customer preferences API',
  changedFiles: [],
}

export function PrRiskAnalysisPage() {
  const [inputText, setInputText] = useState(JSON.stringify(SAMPLE_CHANGE_SET, null, 2))
  const [status, setStatus] = useState<Status>('idle')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [result, setResult] = useState<RiskAnalysisResult | null>(null)

  async function handleAnalyze() {
    setStatus('loading')
    setErrorMessage(null)
    setResult(null)

    let changeSet: PullRequestChangeSet
    try {
      changeSet = JSON.parse(inputText) as PullRequestChangeSet
    } catch {
      setStatus('error')
      setErrorMessage('The change data is not valid JSON.')
      return
    }

    try {
      const analysis = await analyzePullRequest(changeSet)
      setResult(analysis)
      setStatus('success')
    } catch (error) {
      setStatus('error')
      if (error instanceof RiskAnalysisValidationError) {
        setErrorMessage([error.message, ...error.errors].join(' '))
      } else if (error instanceof Error) {
        setErrorMessage(error.message)
      } else {
        setErrorMessage('An unexpected error occurred while analyzing the pull request.')
      }
    }
  }

  return (
    <main>
      <h1>AgentGuard — PR Risk Analysis</h1>

      <label htmlFor="pr-change-data">Pull request change data (JSON)</label>
      <textarea
        id="pr-change-data"
        value={inputText}
        onChange={(event) => setInputText(event.target.value)}
        rows={12}
      />

      <button type="button" onClick={handleAnalyze} disabled={status === 'loading'}>
        {status === 'loading' ? 'Analyzing…' : 'Analyze'}
      </button>

      {status === 'error' && errorMessage && (
        <p role="alert" data-testid="analysis-error">
          {errorMessage}
        </p>
      )}

      {status === 'success' && result && (
        <section aria-label="PR risk analysis result" data-testid="analysis-result">
          <RiskSummary result={result} />
          <ChecksSummary checks={result.checks} />
          <FindingsList findings={result.findings} />
        </section>
      )}
    </main>
  )
}
