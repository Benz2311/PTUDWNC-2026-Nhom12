const AUTH_STORAGE_KEYS = {
  accessToken: 'culinary_access_token',
  refreshToken: 'culinary_refresh_token',
};

export type ApiError = Error & {
  status?: number;
  errors?: Record<string, string[]>;
};

function readToken(): string | null {
  if (typeof window === 'undefined') {
    return null;
  }

  return localStorage.getItem(AUTH_STORAGE_KEYS.accessToken);
}

export function saveAuth(payload: { accessToken?: string; refreshToken?: string }): void {
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

export function clearAuth(): void {
  if (typeof window === 'undefined') {
    return;
  }

  localStorage.removeItem(AUTH_STORAGE_KEYS.accessToken);
  localStorage.removeItem(AUTH_STORAGE_KEYS.refreshToken);
}

export async function apiFetch<T = unknown>(
  url: string,
  options: RequestInit = {},
  withAuth = true
): Promise<T> {
  const headers = new Headers(options.headers ?? {});

  if (withAuth) {
    const token = readToken();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
  }

  const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';
  const fullUrl = `${baseUrl.replace(/\/+$/, '')}${url.startsWith('/') ? url : `/${url}`}`;

  const response = await fetch(fullUrl, {
    ...options,
    headers,
  });

  if (!response.ok) {
    let message = 'Yêu cầu không thành công.';
    let validationErrors: Record<string, string[]> | undefined;

    try {
      const payload = await response.json();
      if (payload) {
        if (payload.errors && typeof payload.errors === 'object') {
          validationErrors = payload.errors as Record<string, string[]>;
          const firstKey = Object.keys(validationErrors)[0];
          if (firstKey && validationErrors[firstKey]?.[0]) {
            message = validationErrors[firstKey][0];
          }
        } else if (typeof payload.message === 'string') {
          message = payload.message;
        } else if (typeof payload.error === 'string') {
          message = payload.error;
        } else if (typeof payload.title === 'string') {
          message = payload.title;
        }
      }
    } catch {
      // Ignore JSON parse failure and keep default message.
    }

    const error = new Error(message) as ApiError;
    error.status = response.status;
    error.errors = validationErrors;
    throw error;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function apiJson<T = unknown>(
  url: string,
  options: RequestInit = {},
  withAuth = true
): Promise<T> {
  return apiFetch<T>(url, options, withAuth);
}
