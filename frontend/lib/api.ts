const AUTH_STORAGE_KEYS = {
  accessToken: 'culinary_access_token',
  refreshToken: 'culinary_refresh_token',
};

export type ApiError = Error & { status?: number };
export const AUTH_CHANGE_EVENT = 'culinary-auth-change';

let refreshInFlight: Promise<boolean> | null = null;

function readToken() {
  if (typeof window === 'undefined') {
    return null;
  }

  return localStorage.getItem(AUTH_STORAGE_KEYS.accessToken);
}

export function saveAuth(payload: { accessToken?: string; refreshToken?: string }) {
  if (typeof window === 'undefined') {
    return;
  }

  if (payload.accessToken) {
    localStorage.setItem(AUTH_STORAGE_KEYS.accessToken, payload.accessToken);
  }

  if (payload.refreshToken) {
    localStorage.setItem(AUTH_STORAGE_KEYS.refreshToken, payload.refreshToken);
  }

  window.dispatchEvent(new Event(AUTH_CHANGE_EVENT));
}

export function clearAuth() {
  if (typeof window === 'undefined') {
    return;
  }

  localStorage.removeItem(AUTH_STORAGE_KEYS.accessToken);
  localStorage.removeItem(AUTH_STORAGE_KEYS.refreshToken);
  window.dispatchEvent(new Event(AUTH_CHANGE_EVENT));
}

async function refreshAuth(): Promise<boolean> {
  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      const refreshToken = localStorage.getItem(AUTH_STORAGE_KEYS.refreshToken);
      if (!refreshToken) {
        clearAuth();
        return false;
      }

      try {
        const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}/api/v1/auth/refresh`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken }),
        });

        if (!response.ok) {
          if (response.status === 401) {
            clearAuth();
          }
          return false;
        }

        const payload = await response.json() as { accessToken?: string; refreshToken?: string };
        if (!payload.accessToken || !payload.refreshToken) {
          clearAuth();
          return false;
        }

        saveAuth(payload);
        return true;
      } catch {
        return false;
      }
    })().finally(() => {
      refreshInFlight = null;
    });
  }

  return refreshInFlight;
}

export async function apiFetchWithResponse<T = unknown>(url: string, options: RequestInit = {}, withAuth = true): Promise<{ data: T; headers: Headers }> {
  const headers = new Headers(options.headers ?? {});

  if (withAuth) {
    const token = readToken();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
  }

  const request = () => fetch(`${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}${url}`, {
    ...options,
    headers,
  });
  let response = await request();

  if (response.status === 401 && withAuth && readToken() && await refreshAuth()) {
    const refreshedToken = readToken();
    if (refreshedToken) {
      headers.set('Authorization', `Bearer ${refreshedToken}`);
      response = await request();
    }
  }

  if (!response.ok) {
    let message = 'Yêu cầu không thành công.';
    try {
      const payload = await response.json();
      if (response.status === 409) {
        message = 'Công thức đã được thay đổi bởi một yêu cầu khác. Hãy tải lại dữ liệu rồi thử lại.';
      } else if (payload && typeof payload.message === 'string') {
        message = payload.message;
      } else if (payload && typeof payload.error === 'string') {
        message = payload.error;
      } else if (payload && typeof payload.detail === 'string') {
        message = payload.detail;
      } else if (payload && typeof payload.title === 'string') {
        message = payload.title;
      }
    } catch {
      // Ignore JSON parse failure and keep default message.
    }

    const error = new Error(message) as ApiError;
    error.status = response.status;
    throw error;
  }

  const data = response.status === 204 ? undefined as T : await response.json() as T;
  return { data, headers: response.headers };
}

export async function apiFetch<T = unknown>(url: string, options: RequestInit = {}, withAuth = true): Promise<T> {
  const response = await apiFetchWithResponse<T>(url, options, withAuth);
  return response.data;
}

export async function apiJson<T = unknown>(url: string, options: RequestInit = {}, withAuth = true): Promise<T> {
  return apiFetch<T>(url, options, withAuth);
}
