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
import { useAuthStore } from "@/lib/auth-store";
import { isTokenExpired } from "@/lib/jwt";
import type {
  ChangePasswordRequest,
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
  googleLogin: (idToken: string) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  updateProfile: (request: UpdateProfileRequest) => Promise<User>;
  changePassword: (request: ChangePasswordRequest) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isInitializing, setIsInitializing] = useState(true);
  const user = useAuthStore((state) => state.user);
  const accessToken = useAuthStore((state) => state.accessToken);
  const isLoading = useAuthStore((state) => state.isLoading);
  const setTokens = useAuthStore((state) => state.setTokens);
  const setUser = useAuthStore((state) => state.setUser);
  const clear = useAuthStore((state) => state.clear);
  const setLoading = useAuthStore((state) => state.setLoading);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      useAuthStore.getState().hydrate();
      const token = authStorage.getAccessToken();
      const refreshToken = authStorage.getRefreshToken();

      // 1. Nếu không có accessToken lẫn refreshToken: Guest thuần túy, kết thúc ngay lập tức, KHÔNG gọi /auth/me
      if (!token && !refreshToken) {
        setLoading(false);
        if (!cancelled) {
          setIsInitializing(false);
        }
        return;
      }

      let activeToken = token;

      // 2. Nếu accessToken đã hết hạn hoặc định dạng không hợp lệ
      if (token && isTokenExpired(token)) {
        if (refreshToken) {
          try {
            const refreshRes = await authApi.refreshToken(refreshToken);
            activeToken = refreshRes.accessToken;
            authStorage.setTokens(refreshRes.accessToken, refreshRes.refreshToken);
            if (!cancelled) {
              setTokens(refreshRes.accessToken, refreshRes.refreshToken);
            }
          } catch {
            authStorage.clear();
            if (!cancelled) {
              clear();
              setLoading(false);
              setIsInitializing(false);
            }
            return;
          }
        } else {
          // Token hết hạn mà không có refresh token -> dọn dẹp sạch và chuyển về Guest
          authStorage.clear();
          if (!cancelled) {
            clear();
            setLoading(false);
            setIsInitializing(false);
          }
          return;
        }
      }

      if (!activeToken) {
        setLoading(false);
        if (!cancelled) {
          setIsInitializing(false);
        }
        return;
      }

      // 3. Token hợp lệ: Tải Profile người dùng hiện tại
      try {
        const me = await authApi.getMe();
        if (!cancelled) {
          setUser(me);
          setTokens(activeToken, authStorage.getRefreshToken());
        }
      } catch {
        if (!cancelled) {
          clear();
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
          setIsInitializing(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [clear, setLoading, setTokens, setUser]);

  useEffect(() => {
    const handleSessionExpired = () => {
      clear();
    };

    window.addEventListener("auth:session-expired", handleSessionExpired);
    return () =>
      window.removeEventListener("auth:session-expired", handleSessionExpired);
  }, [clear]);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await authApi.login(request);
    setTokens(response.accessToken, response.refreshToken);
    setUser(response.user);
  }, [setTokens, setUser]);

  const googleLogin = useCallback(async (idToken: string) => {
    const response = await authApi.googleLogin(idToken);
    setTokens(response.accessToken, response.refreshToken);
    setUser(response.user);
  }, [setTokens, setUser]);

  const register = useCallback(async (request: RegisterRequest) => {
    const response = await authApi.register(request);
    setTokens(response.accessToken, response.refreshToken);
    setUser(response.user);
  }, [setTokens, setUser]);

  const logout = useCallback(async () => {
    const refreshToken = authStorage.getRefreshToken();
    try {
      if (refreshToken) {
        await authApi.logout(refreshToken);
      }
    } finally {
      clear();
    }
  }, [clear]);

  const updateProfile = useCallback(async (request: UpdateProfileRequest) => {
    const updated = await authApi.updateProfile(request);
    setUser(updated);
    return updated;
  }, [setUser]);

  const changePassword = useCallback(async (request: ChangePasswordRequest) => {
    await authApi.changePassword(request);
  }, []);

  const effectiveLoading = isLoading || isInitializing;

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      accessToken,
      isAuthenticated: Boolean(user && accessToken),
      isLoading: effectiveLoading,
      login,
      googleLogin,
      register,
      logout,
      updateProfile,
      changePassword,
    }),
    [user, accessToken, effectiveLoading, login, googleLogin, register, logout, updateProfile, changePassword]
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
