import { create } from "zustand";
import { persist } from "zustand/middleware";
import { authStorage } from "@/lib/auth-storage";
import { getRolesFromToken, isTokenExpired } from "@/lib/jwt";
import type { User } from "@/types/auth";

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  roles: string[];
  isAuthenticated: boolean;
  isLoading: boolean;
  setUser: (user: User | null) => void;
  setTokens: (accessToken: string, refreshToken?: string | null) => void;
  clear: () => void;
  setLoading: (loading: boolean) => void;
  hydrate: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      roles: [],
      isAuthenticated: false,
      isLoading: false,
      setUser: (user) => set({ user, isAuthenticated: Boolean(user) }),
      setTokens: (accessToken, refreshToken) => {
        authStorage.setTokens(accessToken, refreshToken ?? null);
        set({
          accessToken,
          refreshToken: refreshToken ?? null,
          roles: getRolesFromToken(accessToken),
          isAuthenticated: !isTokenExpired(accessToken),
        });
      },
      clear: () => {
        authStorage.clear();
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          roles: [],
          isAuthenticated: false,
        });
      },
      setLoading: (loading) => set({ isLoading: loading }),
      hydrate: () => {
        const accessToken = authStorage.getAccessToken();
        const refreshToken = authStorage.getRefreshToken();
        set({
          accessToken,
          refreshToken,
          roles: getRolesFromToken(accessToken),
          isAuthenticated: Boolean(accessToken) && !isTokenExpired(accessToken),
        });
      },
    }),
    {
      name: "culinary-auth-store",
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        roles: state.roles,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);
