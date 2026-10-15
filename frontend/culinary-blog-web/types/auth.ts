import type { ProblemDetails, ApiErrorResponse } from "./api";
/**
 * TypeScript Types cho module Auth/User — đối chiếu trực tiếp với DTO/Response của Backend.
 * Hợp đồng REST API: Chương 8.1 SRS_Culinary_Blog_v1.0.0
 */

/** Role hợp lệ trong hệ thống (RoleType enum của Backend). */
export type RoleType = "Guest" | "Author" | "Admin";

/**
 * User — khớp `UserResponse` (Backend `UserResponse` DTO).
 * Không chứa PasswordHash, Security Stamp, Access Token, Refresh Token hay trường nội bộ.
 */
export interface User {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
  bio?: string | null;
  emailConfirmed: boolean;
  roles: RoleType[];
  isActive: boolean;
  createdAt: string;
}

/**
 * AuthenticatedUser — user đang đăng nhập kèm access token.
 * Khớp `AuthResponse.User` + `AuthResponse.AccessToken`.
 */
export interface AuthenticatedUser extends User {
  accessToken: string;
}

/**
 * Profile — cấu trúc trả về từ `GET /api/v1/auth/me` và `PATCH /api/v1/auth/me`.
 * Khớp `UserResponse` (cùng contract với `User`).
 */
export type Profile = User;

/** AuthResponse — khớp `AuthResponse` DTO của Backend (login/register/refresh). */
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  userNameOrEmail: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  displayName?: string;
}

/** RefreshTokenRequest — khớp `RefreshRequest` DTO (body của /auth/refresh, /auth/logout, /auth/revoke). */
export interface RefreshTokenRequest {
  refreshToken: string;
}

/** @deprecated Use `RefreshTokenRequest` — giữ tương thích với code hiện có. */
export type RefreshRequest = RefreshTokenRequest;

export interface UpdateProfileRequest {
  displayName: string;
  avatarUrl?: string | null;
  bio?: string | null;
}

export interface ConfirmEmailRequest {
  token: string;
}

export interface ResendConfirmationEmailRequest {
  email: string;
}

export interface GoogleLoginRequest {
  idToken: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

/**
 * ApiProblemDetails — cấu trúc lỗi RFC 7807 trả về từ Backend.
 * Khớp `ProblemDetails` của ASP.NET Core.
 */
export type ApiProblemDetails = ProblemDetails;

/** AuthError — lỗi xác thực/phân quyền đã chuẩn hóa (từ ApiError của api client). */
export type AuthError = ApiErrorResponse;
