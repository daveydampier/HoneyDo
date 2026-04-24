import { describe, it, expect, beforeEach } from 'vitest'
import { authStorage } from './authStorage'

const MOCK_AUTH = { token: 'tok123', profileId: 'pid-abc', displayName: 'Alice' }

describe('authStorage', () => {
  beforeEach(() => localStorage.clear())

  describe('load', () => {
    it('returns all nulls when storage is empty', () => {
      expect(authStorage.load()).toEqual({ token: null, profileId: null, displayName: null })
    })

    it('returns saved values after save', () => {
      authStorage.save(MOCK_AUTH)
      expect(authStorage.load()).toEqual(MOCK_AUTH)
    })
  })

  describe('save', () => {
    it('persists all three fields to localStorage', () => {
      authStorage.save(MOCK_AUTH)
      expect(localStorage.getItem('token')).toBe('tok123')
      expect(localStorage.getItem('profileId')).toBe('pid-abc')
      expect(localStorage.getItem('displayName')).toBe('Alice')
    })

    it('overwrites previously saved values', () => {
      authStorage.save(MOCK_AUTH)
      authStorage.save({ token: 'new-tok', profileId: 'new-pid', displayName: 'Bob' })
      expect(authStorage.load()).toEqual({ token: 'new-tok', profileId: 'new-pid', displayName: 'Bob' })
    })
  })

  describe('clear', () => {
    it('removes all auth keys from localStorage', () => {
      authStorage.save(MOCK_AUTH)
      authStorage.clear()
      expect(authStorage.load()).toEqual({ token: null, profileId: null, displayName: null })
    })

    it('is a no-op when storage is already empty', () => {
      expect(() => authStorage.clear()).not.toThrow()
    })
  })

  describe('getToken', () => {
    it('returns null when no token is stored', () => {
      expect(authStorage.getToken()).toBeNull()
    })

    it('returns the stored token', () => {
      authStorage.save(MOCK_AUTH)
      expect(authStorage.getToken()).toBe('tok123')
    })

    it('returns null after clear', () => {
      authStorage.save(MOCK_AUTH)
      authStorage.clear()
      expect(authStorage.getToken()).toBeNull()
    })
  })
})
