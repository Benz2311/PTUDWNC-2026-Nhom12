"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import type { ReactNode } from "react";
import { authApi } from "@/lib/api/auth";
import { authStorage } from "@/lib/auth-storage";
import type {
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  User,
} from "@/types/auth";

interface AuthContextValue {
  user: User | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  updateProfile: (request: UpdateProfileRequest) => Promise<User>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Auth Provider — quản lý trạng thái xác thực của ứng dụng:
 * - Lưu JWT Token vào localStorage
 * - Tự động gọi GET /api/v1/auth/me khi mở ứng dụng (nếu có token)
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Khởi tạo: nếu có token trong localStorage, tải thông tin user
  useEffect(() => {
    let cancelled = false;

    (async () => {
      const token = authStorage.getAccessToken();
      if (!token) {
        setIsLoading(false);
        return;
      }

      try {
        const me = await authApi.getMe();
        if (!cancelled) {
          setUser(me);
          setAccessToken(token);
        }
      } catch {
        // Token hết hạn hoặc bị thu hồi — đăng xuất im lặng
        if (!cancelled) {
          authStorage.clear();
          setUser(null);
          setAccessToken(null);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  // Lắng nghe sự kiện phiên hết hạn từ Axios Silent Refresh interceptor
  useEffect(() => {
    const handleSessionExpired = () => {
      setUser(null);
      setAccessToken(null);
    };

    window.addEventListener("auth:session-expired", handleSessionExpired);
    return () =>
      window.removeEventListener("auth:session-expired", handleSessionExpired);
  }, []);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await authApi.login(request);
    authStorage.setTokens(response.accessToken, response.refreshToken);
    setAccessToken(response.accessToken);
    setUser(response.user);
  }, []);

  const register = useCallback(async (request: RegisterRequest) => {
    const response = await authApi.register(request);
    authStorage.setTokens(response.accessToken, response.refreshToken);
    setAccessToken(response.accessToken);
    setUser(response.user);
  }, []);

  const logout = useCallback(async () => {
    const refreshToken = authStorage.getRefreshToken();
    try {
      if (refreshToken) {
        await authApi.revoke(refreshToken);
      }
    } finally {
      authStorage.clear();
      setUser(null);
      setAccessToken(null);
    }
  }, []);

  const updateProfile = useCallback(async (request: UpdateProfileRequest) => {
    const updated = await authApi.updateProfile(request);
    setUser(updated);
    return updated;
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      accessToken,
      isAuthenticated: Boolean(user && accessToken),
      isLoading,
      login,
      register,
      logout,
      updateProfile,
    }),
    [user, accessToken, isLoading, login, register, logout, updateProfile]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
