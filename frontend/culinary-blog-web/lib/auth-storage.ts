const AUTH_STORAGE_KEYS = {
  accessToken: "culinary_access_token",
  refreshToken: "culinary_refresh_token",
} as const;

function safeStorage(): Storage | null {
  try {
    if (typeof window === "undefined") {
      return null;
    }
    return window.localStorage;
  } catch {
    return null;
  }
}

/**
 * Lưu trữ JWT Token vào localStorage.
 * Truy cập qua safeStorage() để tránh lỗi khi render phía server (SSR).
 */
export const authStorage = {
  getAccessToken(): string | null {
    return safeStorage()?.getItem(AUTH_STORAGE_KEYS.accessToken) ?? null;
  },

  getRefreshToken(): string | null {
    return safeStorage()?.getItem(AUTH_STORAGE_KEYS.refreshToken) ?? null;
  },

  setTokens(accessToken: string, refreshToken?: string | null): void {
    const storage = safeStorage();
    if (!storage) {
      return;
    }

    storage.setItem(AUTH_STORAGE_KEYS.accessToken, accessToken);
    if (refreshToken) {
      storage.setItem(AUTH_STORAGE_KEYS.refreshToken, refreshToken);
    }
  },

  clear(): void {
    const storage = safeStorage();
    if (!storage) {
      return;
    }

    storage.removeItem(AUTH_STORAGE_KEYS.accessToken);
    storage.removeItem(AUTH_STORAGE_KEYS.refreshToken);
  },
};
