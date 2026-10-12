const AUTH_STORAGE_KEYS = {
  accessToken: 'culinary_access_token',
  refreshToken: 'culinary_refresh_token',
};

export class ApiError extends Error {
  status?: number;
  errors?: Record<string, string[]>;
  data?: any;

  constructor(
    message: string,
    status?: number,
    errors?: Record<string, string[]>,
    data?: any
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
    this.data = data;
  }
}

export const AUTH_CHANGE_EVENT = 'culinary-auth-change';

let refreshInFlight: Promise<boolean> | null = null;

export function getApiBaseUrl(): string {
  if (typeof window !== 'undefined' && process.env.NEXT_PUBLIC_API_URL) {
    return process.env.NEXT_PUBLIC_API_URL.replace(/\/+$/, '');
  }
  return process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
}

export function getAuthToken(): string | null {
  return readToken();
}

function readToken(): string | null {
  if (typeof window === 'undefined') {
    return null;
  }

  return (
    localStorage.getItem(AUTH_STORAGE_KEYS.accessToken) ||
    localStorage.getItem('token') ||
    localStorage.getItem('accessToken')
  );
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

  window.dispatchEvent(new Event(AUTH_CHANGE_EVENT));
}

export function clearAuth(): void {
  if (typeof window === 'undefined') {
    return;
  }

  localStorage.removeItem(AUTH_STORAGE_KEYS.accessToken);
  localStorage.removeItem(AUTH_STORAGE_KEYS.refreshToken);
  localStorage.removeItem('token');
  localStorage.removeItem('accessToken');
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
        const baseUrl = getApiBaseUrl();
        const response = await fetch(`${baseUrl.replace(/\/+$/, '')}/api/v1/auth/refresh`, {
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

        const payload = (await response.json()) as { accessToken?: string; refreshToken?: string };
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

export async function apiFetchWithResponse<T = unknown>(
  url: string,
  options: RequestInit = {},
  withAuth = true
): Promise<{ data: T; headers: Headers }> {
  const headers = new Headers(options.headers ?? {});

  if (withAuth) {
    const token = readToken();
    if (token && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${token}`);
    }
  }

  const baseUrl = getApiBaseUrl();
  const fullUrl = `${baseUrl.replace(/\/+$/, '')}${url.startsWith('/') ? url : `/${url}`}`;

  const request = () =>
    fetch(fullUrl, {
      ...options,
      headers,
    });

  let response = await request();

  if (response.status === 401 && withAuth && readToken() && (await refreshAuth())) {
    const refreshedToken = readToken();
    if (refreshedToken) {
      headers.set('Authorization', `Bearer ${refreshedToken}`);
      response = await request();
    }
  }

  if (!response.ok) {
    let message = 'Yêu cầu không thành công.';
    let validationErrors: Record<string, string[]> | undefined;
    let payload: any = null;

    try {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        payload = await response.json();
      } else {
        const text = await response.text();
        if (text) message = text;
      }

      if (response.status === 409) {
        message = 'Công thức đã được thay đổi bởi một yêu cầu khác. Hãy tải lại dữ liệu rồi thử lại.';
      } else if (payload) {
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
        } else if (typeof payload.detail === 'string') {
          message = payload.detail;
        } else if (typeof payload.title === 'string') {
          message = payload.title;
        }
      }
    } catch {
      // Ignore JSON parse failure and keep default message.
    }

    throw new ApiError(message, response.status, validationErrors, payload);
  }

  const data = response.status === 204 ? (undefined as T) : ((await response.json()) as T);
  return { data, headers: response.headers };
}

export async function apiFetch<T = unknown>(
  url: string,
  options: RequestInit = {},
  withAuth = true
): Promise<T> {
  const response = await apiFetchWithResponse<T>(url, options, withAuth);
  return response.data;
}

export async function apiJson<T = unknown>(
  url: string,
  options: RequestInit = {},
  withAuth = true
): Promise<T> {
  return apiFetch<T>(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      ...options.headers,
    },
  }, withAuth);
}
