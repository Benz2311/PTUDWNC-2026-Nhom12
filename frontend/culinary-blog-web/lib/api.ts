/**
 * Shared API client abstraction cho CulinaryBlog Web
 * Hỗ trợ tự động gắn Token, xử lý baseURL và chuẩn hóa lỗi theo RFC 7807 ProblemDetails
 */

export class ApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;
  data?: unknown;

  constructor(message: string, status: number, errors?: Record<string, string[]>, data?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
    this.data = data;
  }
}

export function getApiBaseUrl(): string {
  if (typeof window !== 'undefined' && process.env.NEXT_PUBLIC_API_URL) {
    return process.env.NEXT_PUBLIC_API_URL.replace(/\/+$/, '');
  }
  return process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
}

export function getAuthToken(): string | null {
  if (typeof window === 'undefined') return null;
  try {
    return localStorage.getItem('token') || localStorage.getItem('accessToken');
  } catch {
    return null;
  }
}

export async function apiFetch<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const baseUrl = getApiBaseUrl();
  const cleanEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
  const url = `${baseUrl}${cleanEndpoint}`;

  const headers = new Headers(options.headers || {});

  const token = getAuthToken();
  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    let errorData: any = null;
    let errorMessage = `Yêu cầu thất bại với mã trạng thái ${response.status}`;

    try {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        errorData = await response.json();
        if (errorData) {
          errorMessage =
            errorData.detail ||
            errorData.title ||
            errorData.message ||
            errorData.error ||
            errorMessage;
        }
      } else {
        const text = await response.text();
        if (text) errorMessage = text;
      }
    } catch {
      // Giữ nguyên message mặc định nếu parse thất bại
    }

    throw new ApiError(
      errorMessage,
      response.status,
      errorData?.errors || undefined,
      errorData
    );
  }

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    return (await response.json()) as T;
  }

  return (await response.text()) as unknown as T;
}

export async function apiJson<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  return apiFetch<T>(endpoint, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      ...options.headers,
    },
  });
}
