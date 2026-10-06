"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuth } from "@/contexts/AuthContext";
import { getRolesFromToken } from "@/lib/jwt";

interface ProtectedRouteProps {
  children: React.ReactNode;
  /**
   * Các role được phép truy cập (ví dụ: ["Admin"]).
   * Bỏ trống = chỉ cần đăng nhập.
   */
  roles?: string[];
}

/**
 * Route bảo vệ — chặn truy cập khi chưa đăng nhập (điều hướng về /login),
 * hỗ trợ kiểm tra Role (ví dụ: yêu cầu "Admin").
 */
export default function ProtectedRoute({
  children,
  roles,
}: ProtectedRouteProps) {
  const { isAuthenticated, isLoading, accessToken } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login");
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="h-10 w-10 animate-spin rounded-full border-4 border-gray-200 border-t-teal-600" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  if (roles && roles.length > 0) {
    const userRoles = getRolesFromToken(accessToken);
    const hasRequiredRole = roles.some((role) =>
      userRoles.some(
        (userRole) => userRole.toLowerCase() === role.toLowerCase()
      )
    );

    if (!hasRequiredRole) {
      return (
        <div className="flex min-h-screen items-center justify-center px-4">
          <div className="w-full max-w-md rounded-xl border border-red-200 bg-red-50 p-8 text-center">
            <h1 className="text-xl font-semibold text-red-700">
              Không có quyền truy cập
            </h1>
            <p className="mt-2 text-sm text-red-600">
              Bạn cần quyền {roles.join(", ")} để truy cập trang này.
            </p>
          </div>
        </div>
      );
    }
  }

  return <>{children}</>;
}
