import axios, {
  isAxiosError,
  type AxiosError,
  type InternalAxiosRequestConfig,
} from "axios";
import { authStorage } from "@/lib/auth-storage";
import type { AuthResponse } from "@/types/auth";
import type { ApiResponse, PaginationMeta, PagedResult } from "@/types/api";

/** Helper bóc tách payload từ envelope { data, meta } của Backend theo SRS */
export function unwrapData<T>(response: { data: ApiResponse<T, any> | T }): T {
  const payload = response.data as any;
  if (payload && typeof payload === "object" && "data" in payload) {
    return payload.data as T;
  }
  return payload as T;
}

/** Helper chuyển đổi paged response { data: T[], meta: PaginationMeta } sang PagedResult<T> */
export function unwrapPaged<T>(response: { data: ApiResponse<T[], PaginationMeta> | any } | any): PagedResult<T> {
  if (!response || typeof response !== "object") {
    return {
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false,
    };
  }

  let payload = response;
  let meta: any = response.meta;

  if (response.data !== undefined) {
    if (
      response.data &&
      typeof response.data === "object" &&
      ("data" in response.data || "items" in response.data || "meta" in response.data)
    ) {
      payload = response.data;
      meta = response.data.meta || meta;
    } else {
      payload = response.data;
    }
  }

  if (payload && payload.meta) {
    meta = payload.meta;
  }

  // Case A: payload.data là Array [...], meta là PaginationMeta (Chuẩn SRS Backend)
  if (payload && Array.isArray(payload.data)) {
    const items = payload.data as T[];
    const page = Number(meta?.page ?? 1);
    const pageSize = Number(meta?.pageSize ?? (items.length || 9));
    const totalCount = Number(meta?.total ?? meta?.totalCount ?? items.length);
    const totalPages = Number(
      meta?.totalPages ?? (pageSize > 0 ? Math.ceil(totalCount / pageSize) : 1)
    );
    return {
      items,
      page,
      pageSize,
      totalCount,
      totalPages,
      hasNextPage: Boolean(meta?.hasNextPage ?? (page < totalPages)),
      hasPreviousPage: Boolean(meta?.hasPreviousPage ?? (page > 1)),
    };
  }

  // Case B: payload.items là Array [...] (PagedResultDto)
  if (payload && Array.isArray(payload.items)) {
    const items = payload.items as T[];
    const page = Number(payload.page ?? meta?.page ?? 1);
    const pageSize = Number(payload.pageSize ?? meta?.pageSize ?? (items.length || 9));
    const totalCount = Number(
      payload.totalCount ?? payload.total ?? meta?.total ?? items.length
    );
    const totalPages = Number(
      payload.totalPages ??
        meta?.totalPages ??
        (pageSize > 0 ? Math.ceil(totalCount / pageSize) : 1)
    );
    return {
      items,
      page,
      pageSize,
      totalCount,
      totalPages,
      hasNextPage: Boolean(payload.hasNextPage ?? (page < totalPages)),
      hasPreviousPage: Boolean(payload.hasPreviousPage ?? (page > 1)),
    };
  }

  // Case C: payload chính nó là một Array
  if (Array.isArray(payload)) {
    const items = payload as T[];
    const page = Number(meta?.page ?? 1);
    const pageSize = Number(meta?.pageSize ?? (items.length || 9));
    const totalCount = Number(meta?.total ?? meta?.totalCount ?? items.length);
    const totalPages = Number(
      meta?.totalPages ?? (pageSize > 0 ? Math.ceil(totalCount / pageSize) : 1)
    );
    return {
      items,
      page,
      pageSize,
      totalCount,
      totalPages,
      hasNextPage: Boolean(meta?.hasNextPage ?? (page < totalPages)),
      hasPreviousPage: Boolean(meta?.hasPreviousPage ?? (page > 1)),
    };
  }

  return {
    items: [],
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  };
}

export function getApiBaseUrl(): string {
  // 1. Môi trường Browser
  if (typeof window !== "undefined") {
    const envUrl = process.env.NEXT_PUBLIC_API_URL?.trim();
    if (!envUrl) {
      return "/api/v1";
    }
    // Nếu là relative path
    if (envUrl.startsWith("/")) {
      const clean = envUrl.replace(/\/+$/, "");
      return clean.endsWith("/api/v1") ? clean : clean.endsWith("/api") ? `${clean}/v1` : `${clean}/api/v1`;
    }
    // Nếu là URL tuyệt đối
    try {
      const parsed = new URL(envUrl);
      // Nếu browser đang truy cập cùng host (ví dụ localhost:8088) nhưng envUrl bị bake là port khác (ví dụ 5000):
      if (
        (parsed.hostname === window.location.hostname || parsed.hostname === "localhost" || parsed.hostname === "127.0.0.1") &&
        parsed.port !== window.location.port &&
        window.location.port !== ""
      ) {
        return "/api/v1";
      }
      const clean = envUrl.replace(/\/+$/, "");
      return clean.endsWith("/api/v1") ? clean : clean.endsWith("/api") ? `${clean}/v1` : `${clean}/api/v1`;
    } catch {
      return "/api/v1";
    }
  }

  // 2. Môi trường Server-Side (Node.js SSR trong Docker)
  const internalUrl = process.env.INTERNAL_API_URL?.trim();
  if (internalUrl) {
    const clean = internalUrl.replace(/\/+$/, "");
    return clean.endsWith("/api/v1") ? clean : clean.endsWith("/api") ? `${clean}/v1` : `${clean}/api/v1`;
  }

  const envUrl = process.env.NEXT_PUBLIC_API_URL?.trim();
  if (envUrl) {
    const clean = envUrl.replace(/\/+$/, "");
    return clean.endsWith("/api/v1") ? clean : clean.endsWith("/api") ? `${clean}/v1` : `${clean}/api/v1`;
  }

  return "http://api:8080/api/v1";
}

const BASE_URL = getApiBaseUrl();

/**
 * Axios Instance dùng chung cho toàn bộ ứng dụng.
 * - Request Interceptor: tự động gắn Authorization: Bearer <token>
 * - Response Interceptor 1: Silent Refresh khi nhận 401 (Token Rotation)
 * - Response Interceptor 2: chuẩn hóa lỗi RFC 7807 ProblemDetails thành ApiError
 */
export const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 10000,
});

// Request Interceptor — log requests in development
if (process.env.NODE_ENV === "development") {
  apiClient.interceptors.request.use((config) => {
    console.log(`[API Request] ${config.method?.toUpperCase()} ${config.baseURL}${config.url}`, {
      headers: config.headers,
      data: config.data,
    });
    return config;
  });
}

/** Lỗi API đã chuẩn hóa — mang theo HTTP status + detail theo chuẩn RFC 7807. */
export class ApiError extends Error {
  status?: number;
  title?: string;
  errors?: Record<string, string[]>;
  traceId?: string;

  constructor(
    message: string,
    init?: {
      status?: number;
      title?: string;
      errors?: Record<string, string[]>;
      traceId?: string;
    }
  ) {
    super(message);
    this.name = "ApiError";
    this.status = init?.status;
    this.title = init?.title;
    this.errors = init?.errors;
    this.traceId = init?.traceId;
  }
}

/** Cấu trúc RFC 7807 ProblemDetails trả về từ Backend. */
interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

// Request Interceptor — tự động gắn Bearer Token vào mọi request và cập nhật baseURL động
apiClient.interceptors.request.use((config) => {
  if (typeof window !== "undefined") {
    config.baseURL = getApiBaseUrl();
  }
  const token = authStorage.getAccessToken();
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// --- Silent Refresh (Token Rotation) ---

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null = null): void {
  failedQueue.forEach((promises) => {
    if (error) {
      promises.reject(error);
    } else if (token) {
      promises.resolve(token);
    }
  });
  failedQueue = [];
}

/**
 * Refresh thất bại (hết hạn / Reuse Detection / Revoke Token Family):
 * xóa token, thông báo AuthContext. Chỉ điều hướng nếu đang ở trang bảo vệ.
 */
function handleRefreshFailure(): void {
  authStorage.clear();

  if (typeof window === "undefined") {
    return;
  }

  window.dispatchEvent(new Event("auth:session-expired"));

  const pathname = window.location.pathname;
  const isProtectedPath =
    pathname.startsWith("/profile") ||
    pathname.startsWith("/admin") ||
    pathname.startsWith("/dashboard");

  if (isProtectedPath) {
    window.location.assign("/login");
  }
}

// Response Interceptor 1 — bắt 401, gọi ngầm /auth/refresh rồi retry request
apiClient.interceptors.response.use(
  (response) => {
    if (process.env.NODE_ENV === "development") {
      console.log(`[API Response] ${response.config.method?.toUpperCase()} ${response.config.url}`, {
        status: response.status,
        data: response.data,
      });
    }
    return response;
  },
  async (error: unknown) => {
    if (!isAxiosError(error)) {
      return Promise.reject(error);
    }

    if (process.env.NODE_ENV === "development") {
      const status = error.response?.status;
      const url = error.config?.url || "";
      // Các mã HTTP client thông thường (401, 403, 404) dùng warn để ghi nhận đầy đủ mà không gây Dev Overlay đỏ
      if (status === 401 || status === 403 || status === 404) {
        console.warn(`[API ${status}] ${error.config?.method?.toUpperCase()} ${url}`, {
          status,
          data: error.response?.data,
          message: error.message,
        });
      } else {
        // Các lỗi mạng, 5xx server error thực sự vẫn ghi nhận console.error
        console.error(`[API Error] ${error.config?.method?.toUpperCase()} ${url}`, {
          status,
          data: error.response?.data,
          message: error.message,
        });
      }
    }

    const originalRequest = error.config as
      | (InternalAxiosRequestConfig & { _retry?: boolean })
      | undefined;

    if (
      error.response?.status !== 401 ||
      !originalRequest ||
      originalRequest._retry
    ) {
      return Promise.reject(error);
    }

    // Tránh vòng lặp: chính endpoint refresh cũng trả 401
    if (originalRequest.url?.includes("/auth/refresh") || originalRequest.url?.includes("/auth/refresh-token")) {
      handleRefreshFailure();
      return Promise.reject(error);
    }

    const refreshToken = authStorage.getRefreshToken();
    // Nếu KHÔNG có refreshToken: không gọi refresh rỗng, chỉ dọn storage và reject
    if (!refreshToken) {
      authStorage.clear();
      return Promise.reject(error);
    }

    // Đang refresh: xếp request vào queue, retry khi có token mới
    if (isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      })
        .then((token) => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return apiClient(originalRequest);
        })
        .catch((queueError: unknown) => Promise.reject(queueError));
    }

    originalRequest._retry = true;
    isRefreshing = true;

    try {
      const response = await apiClient.post<ApiResponse<AuthResponse>>("/auth/refresh", {
        refreshToken,
      });
      const authData = unwrapData(response);
      authStorage.setTokens(authData.accessToken, authData.refreshToken);
      processQueue(null, authData.accessToken);
      originalRequest.headers.Authorization = `Bearer ${authData.accessToken}`;
      return apiClient(originalRequest);
    } catch (refreshError) {
      processQueue(refreshError);
      handleRefreshFailure();
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

// Response Interceptor 2 — chuẩn hóa lỗi thành ApiError (RFC 7807)
apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (!isAxiosError<ProblemDetails>(error)) {
      return Promise.reject(error);
    }

    const problem = error.response?.data;
    const status = error.response?.status;

    const message =
      problem?.detail ??
      problem?.title ??
      (error.code === "ECONNABORTED"
        ? "Yêu cầu vượt quá thời gian chờ."
        : "Không thể kết nối đến máy chủ. Vui lòng thử lại sau.");

    return Promise.reject(
      new ApiError(message, {
        status,
        title: problem?.title,
        errors: problem?.errors,
        traceId: problem?.traceId,
      })
    );
  }
);

/** Tiện ích kiểm tra lỗi Axios đã chuẩn hóa (giữ tương thích kiểu). */
export type { AxiosError };

export function getStoredToken(): string | null {
  return authStorage.getAccessToken();
}

export function extractErrorMessage(error: unknown, fallback = "Đã xảy ra lỗi."): string {
  if (error instanceof ApiError) {
    return error.message || fallback;
  }
  if (error instanceof Error) {
    return error.message || fallback;
  }
  if (typeof error === "string") {
    return error;
  }
  return fallback;
}

export function setStoredToken(token: string): void {
  authStorage.setTokens(token);
}

export function removeStoredToken(): void {
  authStorage.clear();
}
