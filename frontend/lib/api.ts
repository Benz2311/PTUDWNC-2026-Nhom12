const AUTH_STORAGE_KEYS = {
  accessToken: 'culinary_access_token',
  refreshToken: 'culinary_refresh_token',
};

export const AUTH_CHANGE_EVENT = 'culinary-auth-change';

export type ApiError = Error & { status?: number; code?: string };

type AuthPayload = { accessToken?: string; refreshToken?: string };

let refreshInFlight: Promise<boolean> | null = null;

function writeAuth(payload: AuthPayload): void {
  const value = unwrapEnvelope<AuthPayload>(payload);
  if (value.accessToken) {
    localStorage.setItem(AUTH_STORAGE_KEYS.accessToken, value.accessToken);
  }
  if (value.refreshToken) {
    localStorage.setItem(AUTH_STORAGE_KEYS.refreshToken, value.refreshToken);
  }
}

function readToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(AUTH_STORAGE_KEYS.accessToken);
}

export function saveAuth(payload: AuthPayload): void {
  if (typeof window === 'undefined') return;

  writeAuth(payload);
  window.dispatchEvent(new Event(AUTH_CHANGE_EVENT));
}

export function clearAuth(): void {
  if (typeof window === 'undefined') return;

  localStorage.removeItem(AUTH_STORAGE_KEYS.accessToken);
  localStorage.removeItem(AUTH_STORAGE_KEYS.refreshToken);
  window.dispatchEvent(new Event(AUTH_CHANGE_EVENT));
}

function unwrapEnvelope<T>(payload: unknown): T {
  if (payload && typeof payload === 'object' && 'data' in payload) {
    const envelope = payload as { data: unknown; meta?: unknown };
    if (!('meta' in envelope) || envelope.meta == null) {
      return envelope.data as T;
    }
  }
  return payload as T;
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
        const response = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}/api/v1/auth/refresh`,
          {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken }),
          },
        );

        if (!response.ok) {
          if (response.status === 401) clearAuth();
          return false;
        }

        const payload = unwrapEnvelope<AuthPayload>(await response.json());
        if (!payload.accessToken || !payload.refreshToken) {
          clearAuth();
          return false;
        }

        writeAuth(payload);
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

function toApiError(response: Response, payload: unknown): ApiError {
  let message = 'Yêu cầu không thành công.';
  if (payload && typeof payload === 'object') {
    const body = payload as Record<string, unknown>;
    if (typeof body.message === 'string') message = body.message;
    else if (typeof body.error === 'string') message = body.error;
    else if (typeof body.detail === 'string') message = body.detail;
    else if (typeof body.title === 'string') message = body.title;
  }

  const error = new Error(message) as ApiError;
  error.status = response.status;
  if (payload && typeof payload === 'object' && !Array.isArray(payload)) {
    const body = payload as Record<string, unknown>;
    if (typeof body.code === 'string') error.code = body.code;
  }
  return error;
}

export async function apiFetchWithResponse<T = unknown>(
  url: string,
  options: RequestInit = {},
  withAuth = true,
): Promise<{ data: T; headers: Headers }> {
  const headers = new Headers(options.headers ?? {});
  if (withAuth) {
    const token = readToken();
    if (token) headers.set('Authorization', 'Bearer ' + token);
  }

  const request = () => fetch(
    `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}${url}`,
    { ...options, headers },
  );

  let response = await request();
  if (response.status === 401 && withAuth && readToken() && await refreshAuth()) {
    const refreshedToken = readToken();
    if (refreshedToken) {
      headers.set('Authorization', 'Bearer ' + refreshedToken);
      response = await request();
    }
  }

  if (!response.ok) {
    let payload: unknown;
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }
    throw toApiError(response, payload);
  }

  const data = response.status === 204
    ? undefined as T
    : unwrapEnvelope<T>(await response.json());
  return { data, headers: response.headers };
}

export async function apiFetch<T = unknown>(
  url: string,
  options: RequestInit = {},
  withAuth = true,
): Promise<T> {
  const response = await apiFetchWithResponse<T>(url, options, withAuth);
  return response.data;
}

export async function apiJson<T = unknown>(
  url: string,
  options: RequestInit = {},
  withAuth = true,
): Promise<T> {
  return apiFetch<T>(url, options, withAuth);
}

export async function downloadApiFile(
  url: string,
  fallbackName: string,
): Promise<void> {
  const token = readToken();
  const response = await fetch(
    `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}${url}`,
    { headers: token ? { Authorization: `Bearer ${token}` } : {} },
  );
  if (!response.ok) {
    let payload: unknown = null;
    try { payload = await response.json(); } catch { /* non-JSON error */ }
    throw toApiError(response, payload);
  }
  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = objectUrl;
  link.download = fallbackName;
  link.click();
  URL.revokeObjectURL(objectUrl);
}
