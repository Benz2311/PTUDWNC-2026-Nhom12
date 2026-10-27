import { apiClient, unwrapData } from "./client";
import type {
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  User,
} from "@/types/auth";
import type { ApiResponse } from "@/types/api";

export const authApi = {
  async login(request: LoginRequest): Promise<AuthResponse> {
    const response = await apiClient.post<ApiResponse<AuthResponse>>("/auth/login", request);
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
