import { afterEach, describe, expect, it, vi } from 'vitest'
import { PrImportError, analyzePullRequestFromReference } from '../src/services/riskAnalysisClient'

function mockFetchOnce(status: number, body: unknown) {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue({
      ok: status >= 200 && status < 300,
      status,
      json: () => Promise.resolve(body),
    }),
  )
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('analyzePullRequestFromReference', () => {
  it('resolves with the analysis result on 200', async () => {
    mockFetchOnce(200, {
      repositoryName: 'chalk',
      prNumber: 688,
      prTitle: 'Fix',
      score: 0,
      classification: 'LOW',
      recommendation: 'SAFE_TO_REVIEW',
      checks: [],
      findings: [],
      partiallyEvaluatedFiles: [],
    })

    const result = await analyzePullRequestFromReference({
      prUrl: 'https://github.com/chalk/chalk/pull/688',
    })

    expect(result.score).toBe(0)
    expect(result.classification).toBe('LOW')
  })

  it('throws PrImportError with retryableWithCredential=true on 404', async () => {
    mockFetchOnce(404, {
      errorType: 'not_found_or_no_access',
      message: 'not found',
      retryableWithCredential: true,
    })

    await expect(
      analyzePullRequestFromReference({ prUrl: 'https://github.com/a/b/pull/1' }),
    ).rejects.toMatchObject({
      name: 'PrImportError',
      errorType: 'not_found_or_no_access',
      retryableWithCredential: true,
    })
  })

  it('throws PrImportError with retryableWithCredential=false on 400', async () => {
    mockFetchOnce(400, {
      errorType: 'invalid_reference',
      message: 'bad url',
      retryableWithCredential: false,
    })

    const error = await analyzePullRequestFromReference({ prUrl: 'not a url' }).catch(
      (e: unknown) => e,
    )

    expect(error).toBeInstanceOf(PrImportError)
    expect((error as PrImportError).retryableWithCredential).toBe(false)
  })

  it('throws PrImportError on 429', async () => {
    mockFetchOnce(429, {
      errorType: 'rate_limited',
      message: 'rate limited',
      retryableWithCredential: false,
    })

    await expect(
      analyzePullRequestFromReference({ prUrl: 'https://github.com/a/b/pull/1' }),
    ).rejects.toMatchObject({ errorType: 'rate_limited' })
  })

  it('throws a generic Error on an unexpected status', async () => {
    mockFetchOnce(500, {})

    await expect(
      analyzePullRequestFromReference({ prUrl: 'https://github.com/a/b/pull/1' }),
    ).rejects.toThrow('status 500')
  })
})
