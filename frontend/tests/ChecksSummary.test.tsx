import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ChecksSummary } from '../src/components/ChecksSummary'
import { cleanResult, multiRuleResult } from './fixtures/riskAnalysisResults'

describe('ChecksSummary', () => {
  it('marks the two tripped checks failed and the remaining three passed', () => {
    render(<ChecksSummary checks={multiRuleResult.checks} />)

    const items = screen.getAllByRole('listitem')
    expect(items).toHaveLength(5)

    const failed = items.filter((item) => item.textContent?.includes('Failed'))
    const passed = items.filter((item) => item.textContent?.includes('Passed'))
    expect(failed).toHaveLength(2)
    expect(passed).toHaveLength(3)

    expect(screen.getByText('Large Change Size').closest('li')?.textContent).toContain('Failed')
    expect(
      screen.getByText('API Contract Breaking Change').closest('li')?.textContent,
    ).toContain('Failed')
  })

  it('marks all five checks passed when no rule is tripped', () => {
    render(<ChecksSummary checks={cleanResult.checks} />)

    const items = screen.getAllByRole('listitem')
    expect(items).toHaveLength(5)
    items.forEach((item) => expect(item.textContent).toContain('Passed'))
  })
})
