import { apiClient, unwrapData } from "./client";
import type {
  AuthResponse,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  UpdateProfileRequest,
  User,
} from "@/types/auth";
import type { ApiResponse } from "@/types/api";

export const authApi = {
  async login(request: LoginRequest): Promise<AuthResponse> {
    const response = await apiClient.post<ApiResponse<AuthResponse>>("/auth/login", request);
    return unwrapData(response);
  },

  async googleLogin(idToken: string): Promise<AuthResponse> {
    const response = await apiClient.post<ApiResponse<AuthResponse>>("/auth/google", {
      idToken,
    });
    return unwrapData(response);
  },

  async register(request: RegisterRequest): Promise<AuthResponse> {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      "/auth/register",
      request
    );
    return unwrapData(response);
  },

  async getMe(): Promise<User> {
    const response = await apiClient.get<ApiResponse<User>>("/auth/me");
    return unwrapData(response);
  },

  async updateProfile(request: UpdateProfileRequest): Promise<User> {
    const response = await apiClient.patch<ApiResponse<User>>("/auth/me", request);
    return unwrapData(response);
  },

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    const response = await apiClient.post<ApiResponse<AuthResponse>>("/auth/refresh", {
      refreshToken,
    });
    return unwrapData(response);
  },

  async logout(refreshToken: string): Promise<void> {
    await apiClient.post("/auth/logout", { refreshToken });
  },

  async resendVerificationEmail(email: string): Promise<void> {
    await apiClient.post("/auth/email/resend", { email });
  },

  async confirmEmail(token: string): Promise<void> {
    await apiClient.post("/auth/email/confirm", { token });
  },

  async forgotPassword(request: ForgotPasswordRequest): Promise<void> {
    await apiClient.post("/auth/forgot-password", request);
  },

  async changePassword(request: ChangePasswordRequest): Promise<void> {
    await apiClient.post("/auth/change-password", request);
  },

  async resetPassword(request: ResetPasswordRequest): Promise<void> {
    await apiClient.post("/auth/reset-password", request);
  },
};
