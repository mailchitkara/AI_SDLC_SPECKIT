import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { FindingsList } from '../src/components/FindingsList'
import { multiRuleResult } from './fixtures/riskAnalysisResults'

describe('FindingsList', () => {
  it('renders every field for each finding, omitting location when absent', () => {
    render(<FindingsList findings={multiRuleResult.findings} />)

    // LARGE_CHANGE_SIZE finding (no location)
    expect(screen.getByText('Large Change Size')).toBeInTheDocument()
    expect(screen.getByText('642 lines changed across 27 files')).toBeInTheDocument()
    expect(
      screen.getByText('Consider splitting this PR into smaller, focused changes.'),
    ).toBeInTheDocument()

    // API_CONTRACT_BREAKING_CHANGE finding (has a location)
    expect(screen.getByText('API Contract Breaking Change')).toBeInTheDocument()
    expect(screen.getByText('contracts/customer.yaml')).toBeInTheDocument()

    // The location-less finding must not render a location field at all
    const largeChangeCard = screen.getByText('Large Change Size').closest('li')
    expect(largeChangeCard).not.toBeNull()
    expect(largeChangeCard!.textContent).not.toMatch(/Location/i)
  })

  it('filters the list down to a single severity on selection', async () => {
    const user = userEvent.setup()
    render(<FindingsList findings={multiRuleResult.findings} />)

    expect(screen.getByText('Large Change Size')).toBeInTheDocument()
    expect(screen.getByText('API Contract Breaking Change')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText(/filter by severity/i), 'HIGH')

    expect(screen.getByText('API Contract Breaking Change')).toBeInTheDocument()
    expect(screen.queryByText('Large Change Size')).not.toBeInTheDocument()
  })
})
