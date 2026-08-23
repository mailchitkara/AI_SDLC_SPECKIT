import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { RiskSummary } from '../src/components/RiskSummary'
import { blockerResult, cleanResult } from './fixtures/riskAnalysisResults'

describe('RiskSummary', () => {
  it('renders repository, PR, score, classification, and recommendation for a clean PR', () => {
    render(<RiskSummary result={cleanResult} />)

    expect(screen.getByText('agentguard-demo')).toBeInTheDocument()
    expect(screen.getByText(/#1/)).toBeInTheDocument()
    expect(screen.getByText('Update README')).toBeInTheDocument()
    expect(screen.getByText('0')).toBeInTheDocument()
    expect(screen.getByText('LOW')).toBeInTheDocument()
    expect(screen.getByText('SAFE TO REVIEW')).toBeInTheDocument()
  })

  it('renders score 100, CRITICAL, and BLOCK MERGE when a BLOCKER finding is present', () => {
    render(<RiskSummary result={blockerResult} />)

    expect(screen.getByText('100')).toBeInTheDocument()
    expect(screen.getByText('CRITICAL')).toBeInTheDocument()
    expect(screen.getByText('BLOCK MERGE')).toBeInTheDocument()
  })

  // 005-risk-engine-foundation US3 (T027)
  it('shows the mandatory-override note when recommendationForcedByOverride is true', () => {
    render(<RiskSummary result={{ ...blockerResult, recommendationForcedByOverride: true }} />)

    expect(screen.getByRole('note')).toHaveTextContent(/mandatory-override finding/i)
  })

  it('omits the mandatory-override note otherwise', () => {
    render(<RiskSummary result={cleanResult} />)

    expect(screen.queryByRole('note')).not.toBeInTheDocument()
  })
})
