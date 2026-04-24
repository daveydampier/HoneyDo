/**
 * All localStorage reads and writes for auth state live here.
 * Centralising them means swapping to sessionStorage, encryption, or
 * a "remember me" flag only requires changes in this one file.
 */

const TOKEN_KEY       = 'token'
const PROFILE_ID_KEY  = 'profileId'
const DISPLAY_NAME_KEY = 'displayName'

export interface StoredAuth {
  token:       string | null
  profileId:   string | null
  displayName: string | null
}

export const authStorage = {
  /** Load all auth values from storage (called once on app startup). */
  load: (): StoredAuth => ({
    token:       localStorage.getItem(TOKEN_KEY),
    profileId:   localStorage.getItem(PROFILE_ID_KEY),
    displayName: localStorage.getItem(DISPLAY_NAME_KEY),
  }),

  /** Persist a successful auth response. */
  save: (auth: { token: string; profileId: string; displayName: string }): void => {
    localStorage.setItem(TOKEN_KEY,        auth.token)
    localStorage.setItem(PROFILE_ID_KEY,   auth.profileId)
    localStorage.setItem(DISPLAY_NAME_KEY, auth.displayName)
  },

  /** Remove all auth values (logout). */
  clear: (): void => {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(PROFILE_ID_KEY)
    localStorage.removeItem(DISPLAY_NAME_KEY)
  },

  /** Read just the token — used by the API client to attach the Authorization header. */
  getToken: (): string | null => localStorage.getItem(TOKEN_KEY),
}
