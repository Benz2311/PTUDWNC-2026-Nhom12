import axios, { AxiosError } from "axios";

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api/v1";

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 10000,
});

export const AUTH_TOKEN_KEY = "culinary_access_token";

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(AUTH_TOKEN_KEY);
}

export function setStoredToken(token: string): void {
  if (typeof window === "undefined") return;
  localStorage.setItem(AUTH_TOKEN_KEY, token);
}

export function removeStoredToken(): void {
  if (typeof window === "undefined") return;
  localStorage.removeItem(AUTH_TOKEN_KEY);
}

// Request interceptor to inject Authorization header if token exists
apiClient.interceptors.request.use((config) => {
  const token = getStoredToken();
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function extractErrorMessage(error: unknown, fallback = "Đã có lỗi xảy ra."): string {
  if (axios.isAxiosError(error)) {
    const axiosErr = error as AxiosError<ProblemDetails>;
    if (axiosErr.response?.data) {
      const data = axiosErr.response.data;
      if (data.detail) return data.detail;
      if (data.title) return data.title;
      if (data.errors) {
        const errorMessages = Object.values(data.errors).flat();
        if (errorMessages.length > 0) return errorMessages[0];
      }
    }
    if (axiosErr.response?.status === 404) return "Không tìm thấy dữ liệu yêu cầu (404).";
    if (axiosErr.response?.status === 403) return "Bạn không có quyền truy cập tài nguyên này (403).";
    if (axiosErr.response?.status === 409) return "Dữ liệu xung đột hoặc tài nguyên còn ràng buộc (409).";
    if (axiosErr.response?.status === 422) return "Dữ liệu gửi lên không hợp lệ (422).";
  }
  if (error instanceof Error) return error.message;
  return fallback;
}
