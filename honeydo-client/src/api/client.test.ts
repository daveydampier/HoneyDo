import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ApiError, api, setUnauthorizedHandler } from './client'

// ---------------------------------------------------------------------------
// Fetch mock helpers
// ---------------------------------------------------------------------------

const mockFetch = vi.fn()
vi.stubGlobal('fetch', mockFetch)

function makeResponse(status: number, body?: object) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body ?? {}),
  })
}

beforeEach(() => {
  mockFetch.mockReset()
  localStorage.clear()
  setUnauthorizedHandler(null)
})

// ---------------------------------------------------------------------------
// ApiError
// ---------------------------------------------------------------------------

describe('ApiError', () => {
  it('is an instance of Error', () => {
    expect(new ApiError(400, 'Bad Request')).toBeInstanceOf(Error)
  })

  it('name is ApiError', () => {
    expect(new ApiError(400, 'Bad Request').name).toBe('ApiError')
  })

  it('message equals title so Error tooling shows it', () => {
    expect(new ApiError(422, 'Validation Failed').message).toBe('Validation Failed')
  })

  it('exposes status code', () => {
    expect(new ApiError(404, 'Not Found').status).toBe(404)
  })

  it('exposes field-level errors map', () => {
    const err = new ApiError(400, 'Bad Request', { Email: ['Required', 'Invalid'] })
    expect(err.errors?.Email).toEqual(['Required', 'Invalid'])
  })

  it('errors is undefined when not provided', () => {
    expect(new ApiError(500, 'Server Error').errors).toBeUndefined()
  })
})

// ---------------------------------------------------------------------------
// api.get — success paths
// ---------------------------------------------------------------------------

describe('api.get — success', () => {
  it('returns parsed JSON body on 200', async () => {
    mockFetch.mockResolvedValue({ ok: true, status: 200, json: () => Promise.resolve({ id: 1 }) })
    await expect(api.get('/test')).resolves.toEqual({ id: 1 })
  })

  it('returns undefined on 204 without calling json()', async () => {
    const json = vi.fn()
    mockFetch.mockResolvedValue({ ok: true, status: 204, json })
    await expect(api.get('/test')).resolves.toBeUndefined()
    expect(json).not.toHaveBeenCalled()
  })
})

// ---------------------------------------------------------------------------
// api.get — error paths
// ---------------------------------------------------------------------------

describe('api.get — errors', () => {
  it('throws ApiError on 400 with server title and errors', async () => {
    mockFetch.mockResolvedValue(makeResponse(400, { title: 'Validation Failed', errors: { Name: ['Required'] } }))
    const err = await api.get('/test').catch((e: unknown) => e) as ApiError
    expect(err).toBeInstanceOf(ApiError)
    expect(err.status).toBe(400)
    expect(err.title).toBe('Validation Failed')
    expect(err.errors?.Name).toEqual(['Required'])
  })

  it('throws ApiError on 404', async () => {
    mockFetch.mockResolvedValue(makeResponse(404, { title: 'Not Found' }))
    await expect(api.get('/test')).rejects.toBeInstanceOf(ApiError)
  })

  it('falls back to generic title when response body is unparseable', async () => {
    mockFetch.mockResolvedValue({
      ok: false,
      status: 500,
      json: () => Promise.reject(new SyntaxError('bad json')),
    })
    const err = await api.get('/test').catch((e: unknown) => e) as ApiError
    expect(err).toBeInstanceOf(ApiError)
    expect(err.title).toBe('An error occurred')
  })
})

// ---------------------------------------------------------------------------
// 401 interception
// ---------------------------------------------------------------------------

describe('401 interception', () => {
  it('fires onUnauthorized handler when a token is present', async () => {
    localStorage.setItem('token', 'expired-token')
    const handler = vi.fn()
    setUnauthorizedHandler(handler)

    mockFetch.mockResolvedValue(makeResponse(401, { title: 'Unauthorized' }))
    await api.get('/protected').catch(() => {})

    expect(handler).toHaveBeenCalledOnce()
  })

  it('does not fire handler on 401 when no token is present (unauthenticated flows)', async () => {
    const handler = vi.fn()
    setUnauthorizedHandler(handler)

    mockFetch.mockResolvedValue(makeResponse(401, { title: 'Unauthorized' }))
    await api.get('/public').catch(() => {})

    expect(handler).not.toHaveBeenCalled()
  })

  it('still throws ApiError after firing the handler', async () => {
    localStorage.setItem('token', 'expired-token')
    setUnauthorizedHandler(vi.fn())

    mockFetch.mockResolvedValue(makeResponse(401, { title: 'Unauthorized' }))
    await expect(api.get('/protected')).rejects.toBeInstanceOf(ApiError)
  })

  it('does not fire handler on other 4xx errors', async () => {
    localStorage.setItem('token', 'valid-token')
    const handler = vi.fn()
    setUnauthorizedHandler(handler)

    mockFetch.mockResolvedValue(makeResponse(403, { title: 'Forbidden' }))
    await api.get('/protected').catch(() => {})

    expect(handler).not.toHaveBeenCalled()
  })
})
