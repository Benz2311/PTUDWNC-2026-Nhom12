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

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

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
          useAuthStore.getState().setTokens(token, authStorage.getRefreshToken());
          useAuthStore.getState().setUser(me);
        }
      } catch {
        if (!cancelled) {
          authStorage.clear();
          setUser(null);
          setAccessToken(null);
          useAuthStore.getState().clear();
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

  useEffect(() => {
    const handleSessionExpired = () => {
      setUser(null);
      setAccessToken(null);
      useAuthStore.getState().clear();
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
    useAuthStore.getState().setTokens(response.accessToken, response.refreshToken);
    useAuthStore.getState().setUser(response.user);
  }, []);

  const register = useCallback(async (request: RegisterRequest) => {
    const response = await authApi.register(request);
    authStorage.setTokens(response.accessToken, response.refreshToken);
    setAccessToken(response.accessToken);
    setUser(response.user);
    useAuthStore.getState().setTokens(response.accessToken, response.refreshToken);
    useAuthStore.getState().setUser(response.user);
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
      useAuthStore.getState().clear();
    }
  }, []);

  const updateProfile = useCallback(async (request: UpdateProfileRequest) => {
    const updated = await authApi.updateProfile(request);
    setUser(updated);
    useAuthStore.getState().setUser(updated);
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
