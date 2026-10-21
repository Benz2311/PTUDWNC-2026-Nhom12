"use client";

import { useAuthStore } from "@/lib/auth-store";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

interface ProtectedRouteProps {
  children: React.ReactNode;
  roles?: string[];
  fallback?: React.ReactNode;
}

export function ProtectedRoute({ children, roles, fallback }: ProtectedRouteProps) {
  const router = useRouter();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const userRoles = useAuthStore((s) => s.roles);
  const isLoading = useAuthStore((s) => s.isLoading);

  useEffect(() => {
    if (isLoading) return;
    if (!isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (roles && roles.length > 0 && !roles.some((r) => userRoles.includes(r))) {
      router.replace("/403");
    }
  }, [isAuthenticated, isLoading, roles, userRoles, router]);

  if (isLoading) return fallback ?? null;
  if (!isAuthenticated) return null;
  if (roles && roles.length > 0 && !roles.some((r) => userRoles.includes(r))) {
    return null;
  }
  return children as React.ReactElement;
}
