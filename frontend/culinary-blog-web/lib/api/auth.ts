import { apiClient } from "./client";
import type {
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  User,
} from "@/types/auth";

/**
 * Auth API — tương ứng với AuthController (Backend):
 * POST   /api/v1/auth/login              Đăng nhập
 * POST   /api/v1/auth/register           Đăng ký
 * GET    /api/v1/auth/me                 Thông tin người dùng hiện tại
 * PATCH  /api/v1/auth/me                 Cập nhật profile
 * POST   /api/v1/auth/refresh            Làm mới access token (Token Rotation)
 * POST   /api/v1/auth/revoke             Thu hồi refresh token (đăng xuất)
 * POST   /api/v1/auth/email/resend       Gửi lại email xác thực
 * POST   /api/v1/auth/email/confirm      Xác thực email (userId + token)
 */
export const authApi = {
  async login(request: LoginRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>("/auth/login", request);
    return data;
  },

  async register(request: RegisterRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>(
      "/auth/register",
      request
    );
    return data;
  },

  async getMe(): Promise<User> {
    const { data } = await apiClient.get<User>("/auth/me");
    return data;
  },

  async updateProfile(request: UpdateProfileRequest): Promise<User> {
    const { data } = await apiClient.patch<User>("/auth/me", request);
    return data;
  },

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>("/auth/refresh", {
      refreshToken,
    });
    return data;
  },

  async revoke(refreshToken: string): Promise<void> {
    await apiClient.post("/auth/revoke", { refreshToken });
  },

  async resendVerificationEmail(email: string): Promise<void> {
    await apiClient.post("/auth/email/resend", { email });
  },

  async confirmEmail(userId: string, token: string): Promise<void> {
    await apiClient.post("/auth/email/confirm", { userId, token });
  },

  async forgotPassword(request: ForgotPasswordRequest): Promise<void> {
    await apiClient.post("/auth/forgot-password", request);
  },
};
