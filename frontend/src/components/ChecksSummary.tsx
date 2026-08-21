import type { CheckResult } from '../types/riskAnalysis'
import styles from './ChecksSummary.module.css'

interface ChecksSummaryProps {
  checks: CheckResult[]
}

export function ChecksSummary({ checks }: ChecksSummaryProps) {
  return (
    <div className={styles.checks}>
      <h3>Checks</h3>
      <ul className={styles.list}>
        {checks.map((check) => (
          <li key={check.ruleId} className={styles.item}>
            <span className={check.passed ? styles.pass : styles.fail}>
              {check.passed ? 'Passed' : 'Failed'}
            </span>
            <span>{check.ruleName}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}
