const AUTH_STORAGE_KEYS = {
  accessToken: 'culinary_access_token',
  refreshToken: 'culinary_refresh_token',
};

export type ApiError = Error & { status?: number };

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
}

export function clearAuth() {
  if (typeof window === 'undefined') {
    return;
  }

  localStorage.removeItem(AUTH_STORAGE_KEYS.accessToken);
  localStorage.removeItem(AUTH_STORAGE_KEYS.refreshToken);
}

export async function apiFetchWithResponse<T = unknown>(url: string, options: RequestInit = {}, withAuth = true): Promise<{ data: T; headers: Headers }> {
  const headers = new Headers(options.headers ?? {});

  if (withAuth) {
    const token = readToken();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
  }

  const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}${url}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    let message = 'Yêu cầu không thành công.';
    try {
      const payload = await response.json();
      if (payload && typeof payload.message === 'string') {
        message = payload.message;
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
