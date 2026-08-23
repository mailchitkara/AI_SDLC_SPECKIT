import { useMemo, useState } from 'react'
import type { Finding, Severity } from '../types/riskAnalysis'
import { formatEnumLabel } from '../utils/labels'
import styles from './FindingsList.module.css'

interface FindingsListProps {
  findings: Finding[]
}

const SEVERITY_ORDER: Severity[] = ['BLOCKER', 'HIGH', 'MEDIUM', 'LOW', 'INFO']

export function FindingsList({ findings }: FindingsListProps) {
  const [selectedSeverity, setSelectedSeverity] = useState<Severity | 'ALL'>('ALL')

  const availableSeverities = useMemo(
    () => SEVERITY_ORDER.filter((severity) => findings.some((f) => f.severity === severity)),
    [findings],
  )

  const filteredFindings = useMemo(
    () =>
      selectedSeverity === 'ALL'
        ? findings
        : findings.filter((finding) => finding.severity === selectedSeverity),
    [findings, selectedSeverity],
  )

  if (findings.length === 0) {
    return (
      <div className={styles.findings}>
        <h3>Findings</h3>
        <p>No findings — all checks passed.</p>
      </div>
    )
  }

  return (
    <div className={styles.findings}>
      <div className={styles.toolbar}>
        <h3>Findings</h3>
        <div>
          <label htmlFor="severity-filter">Filter by severity</label>
          <select
            id="severity-filter"
            value={selectedSeverity}
            onChange={(event) => setSelectedSeverity(event.target.value as Severity | 'ALL')}
          >
            <option value="ALL">All severities</option>
            {availableSeverities.map((severity) => (
              <option key={severity} value={severity}>
                {severity}
              </option>
            ))}
          </select>
        </div>
      </div>

      {filteredFindings.length === 0 ? (
        <p>No findings match the selected severity.</p>
      ) : (
        <ul className={styles.list}>
          {filteredFindings.map((finding) => (
            <li key={`${finding.ruleId}-${finding.location ?? 'pr-wide'}`} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={`${styles.severityBadge} ${styles[`severity${finding.severity}`]}`}>
                  {finding.severity}
                </span>
                <h4 className={styles.ruleName}>{finding.ruleName}</h4>
              </div>

              {(finding.dimension || finding.confidence) && (
                <div className={styles.metaRow}>
                  {finding.dimension && (
                    <span className={styles.metaBadge}>{formatEnumLabel(finding.dimension)}</span>
                  )}
                  {finding.confidence && (
                    <span className={styles.metaBadge}>{formatEnumLabel(finding.confidence)} confidence</span>
                  )}
                  {finding.mandatoryOverride && (
                    <span className={styles.overrideBadge}>Mandatory block</span>
                  )}
                </div>
              )}

              <p className={styles.explanation}>{finding.explanation}</p>

              <dl className={styles.details}>
                <dt>Evidence</dt>
                <dd>{finding.evidence}</dd>

                {finding.location && (
                  <>
                    <dt>Location</dt>
                    <dd>{finding.location}</dd>
                  </>
                )}

                <dt>Suggested remediation</dt>
                <dd>{finding.remediation}</dd>
              </dl>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

