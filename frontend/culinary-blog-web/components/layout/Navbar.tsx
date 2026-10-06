"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ChefHat, LogOut, ShieldCheck, User as UserIcon } from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/components/ui/toast";
import { getRolesFromToken } from "@/lib/jwt";

export default function Navbar() {
  const { user, accessToken, isAuthenticated, logout } = useAuth();
  const { error: showErrorToast } = useToast();
  const router = useRouter();

  const isAdmin = getRolesFromToken(accessToken).some(
    (role) => role.toLowerCase() === "admin"
  );

  const handleLogout = async () => {
    try {
      await logout();
    } catch {
      showErrorToast("Đăng xuất thất bại, vui lòng thử lại");
    } finally {
      router.push("/login");
      router.refresh();
    }
  };

  return (
    <header className="sticky top-0 z-40 border-b border-teal-700/10 bg-white/95 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6">
        <Link href="/" className="flex items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-teal-600 text-white">
            <ChefHat className="h-5 w-5" />
          </span>
          <span className="text-xl font-bold text-teal-700">Culinary Blog</span>
        </Link>

        <nav className="flex items-center gap-1 sm:gap-2">
          {!isAuthenticated ? (
            <>
              <Link
                href="/login"
                className="rounded-lg px-4 py-2 text-sm font-medium text-teal-700 transition-colors hover:bg-teal-50"
              >
                Đăng nhập
              </Link>
              <Link
                href="/register"
                className="rounded-lg bg-teal-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-teal-700"
              >
                Đăng ký
              </Link>
            </>
          ) : (
            <>
              {user?.avatarUrl ? (
                <img
                  src={user.avatarUrl}
                  alt={user.displayName}
                  className="h-8 w-8 rounded-full object-cover"
                />
              ) : (
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-teal-100 text-sm font-semibold text-teal-700">
                  {(user?.displayName ?? user?.userName ?? "U")
                    .charAt(0)
                    .toUpperCase()}
                </span>
              )}

              <span className="hidden max-w-32 truncate text-sm font-medium text-gray-700 sm:block">
                {user?.displayName || user?.userName}
              </span>

              <Link
                href="/profile"
                className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-teal-50 hover:text-teal-700"
              >
                <UserIcon className="h-4 w-4" />
                <span className="hidden sm:inline">Hồ sơ</span>
              </Link>

              {isAdmin && (
                <Link
                  href="/admin/users"
                  className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-teal-50 hover:text-teal-700"
                >
                  <ShieldCheck className="h-4 w-4" />
                  <span className="hidden sm:inline">Admin</span>
                </Link>
              )}

              <button
                type="button"
                onClick={handleLogout}
                className="flex items-center gap-1.5 rounded-lg border border-teal-700/20 px-3 py-2 text-sm font-medium text-teal-700 transition-colors hover:bg-teal-50"
              >
                <LogOut className="h-4 w-4" />
                <span className="hidden sm:inline">Đăng xuất</span>
              </button>
            </>
          )}
        </nav>
      </div>
    </header>
  );
}
