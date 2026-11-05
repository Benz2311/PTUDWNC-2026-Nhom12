'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch, apiJson, AUTH_CHANGE_EVENT, clearAuth } from '@/lib/api';
import type { ApiError } from '@/lib/api';

type AuthUser = {
  id: string;
  displayName?: string;
  fullName?: string;
  userName?: string;
  email: string;
};

type AuthNavProps = {
  variant?: 'home' | 'flow';
};

let profileRequest: { token: string; promise: Promise<AuthUser> } | null = null;

function getProfile(token: string): Promise<AuthUser> {
  if (profileRequest?.token === token) return profileRequest.promise;

  const promise = apiJson<AuthUser>('/api/v1/auth/me').finally(() => {
    if (profileRequest?.promise === promise) profileRequest = null;
  });
  profileRequest = { token, promise };
  return promise;
}

export default function AuthNav({ variant = 'home' }: AuthNavProps) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const [loadError, setLoadError] = useState(false);

  useEffect(() => {
    let requestVersion = 0;

    async function loadAuthState() {
      const version = ++requestVersion;
      const accessToken = localStorage.getItem('culinary_access_token');
      if (!accessToken) {
        setUser(null);
        setLoadError(false);
        setIsLoading(false);
        return;
      }

      setIsLoading(true);
      setLoadError(false);
      try {
        const profile = await getProfile(accessToken);
        if (version === requestVersion) {
          setUser(profile);
        }
      } catch (error) {
        if (version === requestVersion) {
          if ((error as ApiError).status === 401) {
            clearAuth();
            setUser(null);
          } else {
            setLoadError(true);
          }
        }
      } finally {
        if (version === requestVersion) {
          setIsLoading(false);
        }
      }
    }

    const handleAuthChange = () => void loadAuthState();
    const handleStorageChange = (event: StorageEvent) => {
      if (event.key === 'culinary_access_token' || event.key === null) {
        void loadAuthState();
      }
    };
    window.addEventListener(AUTH_CHANGE_EVENT, handleAuthChange);
    window.addEventListener('storage', handleStorageChange);
    void loadAuthState();

    return () => {
      requestVersion++;
      window.removeEventListener(AUTH_CHANGE_EVENT, handleAuthChange);
      window.removeEventListener('storage', handleStorageChange);
    };
  }, []);

  async function handleLogout() {
    setIsLoggingOut(true);
    try {
      await apiFetch('/api/v1/auth/logout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: localStorage.getItem('culinary_refresh_token') }),
      });
    } catch {
      // Clear the local session even if the API is temporarily unavailable.
    } finally {
      clearAuth();
      setIsLoggingOut(false);
      router.replace('/');
    }
  }

  if (variant === 'flow') {
    return (
      <nav className="flow-nav" aria-label="Điều hướng tài khoản">
        {isLoading ? <span aria-live="polite">Đang tải tài khoản...</span> : user ? (
          <>
            <a href="/profile">Xin chào, {user.displayName || user.fullName || user.userName || user.email}</a>
            <button className="flow-nav-button" type="button" onClick={handleLogout} disabled={isLoggingOut}>
              {isLoggingOut ? 'Đang đăng xuất...' : 'Đăng xuất'}
            </button>
          </>
        ) : loadError ? (
          <a href="/profile">Không thể xác minh phiên</a>
        ) : (
          <><a href="/login">Đăng nhập</a><a href="/register">Đăng ký</a></>
        )}
      </nav>
    );
  }

  return (
    <div className="auth-actions" aria-live="polite">
      {isLoading ? <span className="text-button">Đang tải...</span> : user ? (
        <>
          <a className="text-button" href="/profile">Xin chào, {user.displayName || user.fullName || user.userName || user.email}</a>
          <button className="outline-button" type="button" onClick={handleLogout} disabled={isLoggingOut}>
            {isLoggingOut ? 'Đang đăng xuất...' : 'Đăng xuất'}
          </button>
        </>
      ) : loadError ? (
        <a className="text-button" href="/profile">Không thể xác minh phiên</a>
      ) : (
        <><a className="text-button" href="/login">Đăng nhập</a><a className="primary-button" href="/register">Đăng ký miễn phí</a></>
      )}
    </div>
  );
}
