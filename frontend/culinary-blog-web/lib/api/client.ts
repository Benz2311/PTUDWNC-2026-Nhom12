import axios, {
  isAxiosError,
  type AxiosError,
  type InternalAxiosRequestConfig,
} from "axios";
import { authStorage } from "@/lib/auth-storage";
import type { AuthResponse } from "@/types/auth";

const BASE_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api/v1";

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

// Request Interceptor — tự động gắn Bearer Token vào mọi request
apiClient.interceptors.request.use((config) => {
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
 * xóa token, thông báo AuthContext và chuyển hướng về /login.
 */
function handleRefreshFailure(): void {
  authStorage.clear();

  if (typeof window === "undefined") {
    return;
  }

  window.dispatchEvent(new Event("auth:session-expired"));
  window.location.assign("/login");
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
      console.error(`[API Error] ${error.config?.method?.toUpperCase()} ${error.config?.url}`, {
        status: error.response?.status,
        data: error.response?.data,
        message: error.message,
      });
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

    const refreshToken = authStorage.getRefreshToken();

    try {
      const { data } = await apiClient.post<AuthResponse>("/auth/refresh", {
        refreshToken,
      });
      authStorage.setTokens(data.accessToken, data.refreshToken);
      processQueue(null, data.accessToken);
      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
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
