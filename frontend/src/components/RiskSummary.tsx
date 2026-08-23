import type { RiskAnalysisResult } from '../types/riskAnalysis'
import { formatEnumLabel } from '../utils/labels'
import styles from './RiskSummary.module.css'

interface RiskSummaryProps {
  result: RiskAnalysisResult
}

const CLASSIFICATION_TONE: Record<RiskAnalysisResult['classification'], string> = {
  LOW: styles.toneLow,
  MEDIUM: styles.toneMedium,
  HIGH: styles.toneHigh,
  CRITICAL: styles.toneCritical,
}

export function RiskSummary({ result }: RiskSummaryProps) {
  return (
    <div className={styles.summary}>
      <header className={styles.header}>
        <p className={styles.repository}>{result.repositoryName}</p>
        <h2 className={styles.prTitle}>
          <span className={styles.prNumber}>#{result.prNumber}</span> {result.prTitle}
        </h2>
      </header>

      <div className={styles.scoreRow}>
        <div>
          <span className={styles.label}>Risk score</span>
          <p className={styles.score}>{result.score}</p>
          <span className={`${styles.classification} ${CLASSIFICATION_TONE[result.classification]}`}>
            {result.classification}
          </span>
        </div>

        <div>
          <span className={styles.label}>Recommendation</span>
          <p className={`${styles.recommendation} ${CLASSIFICATION_TONE[result.classification]}`}>
            {formatEnumLabel(result.recommendation)}
          </p>
        </div>
      </div>

      {result.recommendationForcedByOverride && (
        <p className={styles.overrideNote} role="note">
          This recommendation was forced by a mandatory-override finding, independent of the risk score.
        </p>
      )}
    </div>
  )
}
