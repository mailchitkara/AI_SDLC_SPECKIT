import { useState } from 'react'
import type { PullRequestChangeSet, RiskAnalysisResult } from '../types/riskAnalysis'
import {
  analyzePullRequest,
  analyzePullRequestFromReference,
  PrImportError,
  RiskAnalysisValidationError,
} from '../services/riskAnalysisClient'
import { RiskSummary } from '../components/RiskSummary'
import { FindingsList } from '../components/FindingsList'
import { ChecksSummary } from '../components/ChecksSummary'

type Status = 'idle' | 'loading' | 'error' | 'success'
type Mode = 'json' | 'github'

const SAMPLE_CHANGE_SET: PullRequestChangeSet = {
  repositoryName: 'agentguard-demo',
  prNumber: 42,
  prTitle: 'Add customer preferences API',
  changedFiles: [],
}

export function PrRiskAnalysisPage() {
  const [mode, setMode] = useState<Mode>('json')

  const [inputText, setInputText] = useState(JSON.stringify(SAMPLE_CHANGE_SET, null, 2))
  const [prUrl, setPrUrl] = useState('')
  const [credential, setCredential] = useState('')

  const [status, setStatus] = useState<Status>('idle')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [retryableWithCredential, setRetryableWithCredential] = useState(false)
  const [result, setResult] = useState<RiskAnalysisResult | null>(null)

  function switchMode(next: Mode) {
    setMode(next)
    setStatus('idle')
    setErrorMessage(null)
    setRetryableWithCredential(false)
    setResult(null)
  }

  async function handleAnalyzeJson() {
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

  async function handleAnalyzeGitHub() {
    setStatus('loading')
    setErrorMessage(null)
    setRetryableWithCredential(false)
    setResult(null)

    try {
      const analysis = await analyzePullRequestFromReference({
        prUrl,
        credential: credential.trim() === '' ? undefined : credential,
      })
      setResult(analysis)
      setStatus('success')
    } catch (error) {
      setStatus('error')
      if (error instanceof PrImportError) {
        setErrorMessage(error.message)
        setRetryableWithCredential(error.retryableWithCredential)
      } else if (error instanceof Error) {
        setErrorMessage(error.message)
      } else {
        setErrorMessage('An unexpected error occurred while importing the pull request.')
      }
    }
  }

  return (
    <main>
      <h1>AgentGuard — PR Risk Analysis</h1>

      <div role="tablist" aria-label="How to provide PR data">
        <button
          type="button"
          role="tab"
          aria-selected={mode === 'json'}
          onClick={() => switchMode('json')}
        >
          Paste JSON
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={mode === 'github'}
          onClick={() => switchMode('github')}
        >
          GitHub PR URL
        </button>
      </div>

      {mode === 'json' && (
        <>
          <label htmlFor="pr-change-data">Pull request change data (JSON)</label>
          <textarea
            id="pr-change-data"
            value={inputText}
            onChange={(event) => setInputText(event.target.value)}
            rows={12}
          />
          <button type="button" onClick={handleAnalyzeJson} disabled={status === 'loading'}>
            {status === 'loading' ? 'Analyzing…' : 'Analyze'}
          </button>
        </>
      )}

      {mode === 'github' && (
        <>
          <label htmlFor="pr-url">GitHub pull request URL</label>
          <input
            id="pr-url"
            type="text"
            value={prUrl}
            onChange={(event) => setPrUrl(event.target.value)}
            placeholder="https://github.com/{owner}/{repo}/pull/{number}"
          />

          <label htmlFor="pr-credential">
            GitHub credential (optional — required for private repos, or to raise the rate limit)
          </label>
          <input
            id="pr-credential"
            type="password"
            value={credential}
            onChange={(event) => setCredential(event.target.value)}
            placeholder="ghp_…"
          />

          <button
            type="button"
            onClick={handleAnalyzeGitHub}
            disabled={status === 'loading' || prUrl.trim() === ''}
          >
            {status === 'loading' ? 'Analyzing…' : 'Analyze'}
          </button>

          {retryableWithCredential && (
            <p role="note">
              This PR could not be found without a credential — it may be private. Enter a GitHub
              token above with access to it, then try again.
            </p>
          )}
        </>
      )}

      {status === 'error' && errorMessage && (
        <p role="alert" data-testid="analysis-error">
          {errorMessage}
        </p>
      )}

      {status === 'success' && result && (
        <section aria-label="PR risk analysis result" data-testid="analysis-result">
          <RiskSummary result={result} />
          <ChecksSummary checks={result.checks} />
          {result.partiallyEvaluatedFiles && result.partiallyEvaluatedFiles.length > 0 && (
            <p role="note" data-testid="partially-evaluated-files">
              {result.partiallyEvaluatedFiles.length} file(s) could not be fully evaluated (binary
              or too large):{' '}
              {result.partiallyEvaluatedFiles.map((f) => f.path).join(', ')}
            </p>
          )}
          <FindingsList findings={result.findings} />
        </section>
      )}
    </main>
  )
}
