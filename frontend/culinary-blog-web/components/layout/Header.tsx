"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  Search,
  Menu,
  X,
  LogOut,
  PlusCircle,
  ChevronDown,
  Users,
  UserCircle,
  PenTool,
} from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
} from "@/components/ui/DropdownMenu";

interface UserProfile {
  name: string;
  email: string;
  role: "Guest" | "User" | "Author" | "Admin";
}

export function Header() {
  const pathname = usePathname();
  const router = useRouter();
  const { user: authUser, logout } = useAuth();

  const [searchQuery, setSearchQuery] = useState("");
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const user: UserProfile = authUser
    ? {
        name: authUser.displayName || authUser.userName,
        email: authUser.email,
        role: authUser.roles.includes("Admin")
          ? "Admin"
          : authUser.roles.includes("Author")
            ? "Author"
            : "User",
      }
    : { name: "Khách", email: "", role: "Guest" };
  const hasAuthorAccess = user.role === "Author" || user.role === "Admin";

  // Sync search input if on search page
  useEffect(() => {
    if (typeof window !== "undefined" && pathname === "/search") {
      const q = new URLSearchParams(window.location.search).get("q");
      if (q) setSearchQuery(q);
    }
  }, [pathname]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      router.push(`/search?q=${encodeURIComponent(searchQuery.trim())}`);
      setMobileMenuOpen(false);
    }
  };

  const handleLogout = async () => {
    await logout();
    router.refresh();
  };

  const navLinks = [
    { href: "/", label: "Trang chủ" },
    { href: "/categories", label: "Danh mục món ăn" },
    { href: "/recipes", label: "Khám phá công thức" },
    { href: "/dashboard/statistics", label: "Thống kê" },
  ];

  return (
    <header className="sticky top-0 z-40 w-full border-b border-emerald-100 bg-white/95 backdrop-blur-md transition-all shadow-xs">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between gap-3">
          {/* 1. Logo & Brand */}
          <Link href="/" className="flex items-center gap-3 flex-shrink-0 group">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-800 text-white shadow-sm group-hover:bg-emerald-700 transition-colors">
              <svg className="w-6 h-6 text-amber-400" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2a4.5 4.5 0 0 0-4.47 4.03A4.5 4.5 0 0 0 3 10.5a4.5 4.5 0 0 0 3.73 4.43A4 4 0 0 0 10 18h4a4 4 0 0 0 3.27-3.07A4.5 4.5 0 0 0 21 10.5a4.5 4.5 0 0 0-4.53-4.47A4.5 4.5 0 0 0 12 2zm-2 18v2h4v-2h-4z" />
              </svg>
            </div>
            <div>
              <div className="flex items-center gap-1.5">
                <span className="text-xl font-extrabold tracking-tight text-emerald-900">
                  Culinary<span className="text-amber-500">Blog</span>
                </span>
                <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-bold text-emerald-700 border border-emerald-200">
                  Nhóm 12
                </span>
              </div>
              <p className="text-[11px] text-gray-500 hidden sm:block">
                Nền tảng chia sẻ và học nấu ăn
              </p>
            </div>
          </Link>

          {/* 2. Desktop Navigation */}
          <nav className="hidden lg:flex items-center gap-1">
            {navLinks.map((link) => {
              const isActive =
                pathname === link.href ||
                (link.href !== "/" && pathname.startsWith(link.href));
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  className={`px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                    isActive
                      ? "bg-emerald-700 text-white shadow-xs font-semibold"
                      : "text-gray-700 hover:text-emerald-800 hover:bg-emerald-50"
                  }`}
                >
                  {link.label}
                </Link>
              );
            })}
            {user.role === "Admin" && (
              <Link
                href="/admin/users"
                onClick={() => setMobileMenuOpen(false)}
                className={`rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${
                  pathname.startsWith("/admin/users")
                    ? "bg-purple-700 text-white"
                    : "text-purple-700 hover:bg-purple-50"
                }`}
              >
                Quản lý người dùng
              </Link>
            )}
          </nav>

          {/* 3. Search Bar */}
          <form
            onSubmit={handleSearchSubmit}
            className="hidden md:flex items-center relative flex-1 max-w-xs"
          >
            <input
              type="text"
              data-testid="header-search-input"
              placeholder="Tìm công thức, món ăn..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full h-9 pl-9 pr-14 text-xs rounded-full border border-gray-200 bg-gray-50 focus:bg-white focus:border-emerald-600 focus:outline-none focus:ring-2 focus:ring-emerald-600/20 transition-all placeholder:text-gray-400"
            />
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-gray-400 pointer-events-none" />
            <button
              type="submit"
              data-testid="header-search-submit"
              aria-label="Tìm kiếm"
              className="absolute right-1 top-1/2 -translate-y-1/2 px-2.5 py-1 text-[11px] font-bold rounded-full text-white bg-emerald-700 hover:bg-emerald-800 transition-colors shadow-2xs cursor-pointer"
            >
              Tìm
            </button>
          </form>

          {/* 4. Auth & User Profile Section */}
          <div className="flex items-center gap-2">
            {user.role !== "Guest" ? (
              /* User Dropdown Menu */
              <DropdownMenu>
                <DropdownMenuTrigger className="flex items-center gap-2 rounded-full p-1 hover:bg-gray-100 transition-colors">
                  <div className="flex h-9 w-9 items-center justify-center rounded-full bg-emerald-700 text-white font-bold text-sm shadow-xs">
                    {user.name.charAt(0).toUpperCase()}
                  </div>
                  <ChevronDown className="h-4 w-4 text-gray-500 hidden sm:block" />
                </DropdownMenuTrigger>
                <DropdownMenuContent align="right" className="w-56">
                  <DropdownMenuLabel>
                    <div className="font-semibold text-gray-900 truncate">
                      {user.name}
                    </div>
                    <div className="text-[11px] text-gray-500 font-normal truncate">
                      {user.email}
                    </div>
                    <span className="inline-block mt-1 text-[10px] font-semibold uppercase px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800">
                      Vai trò: {user.role}
                    </span>
                  </DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={() => router.push("/profile")}>
                    <UserCircle className="h-4 w-4 text-emerald-700" />
                    <span>Hồ sơ cá nhân</span>
                  </DropdownMenuItem>
                  {hasAuthorAccess && (
                    <DropdownMenuItem onClick={() => router.push("/recipes/create")}>
                      <PlusCircle className="h-4 w-4 text-emerald-700" />
                      <span>Tạo công thức mới</span>
                    </DropdownMenuItem>
                  )}
                  {hasAuthorAccess && (
                    <DropdownMenuItem onClick={() => router.push("/dashboard/recipes")}>
                      <PenTool className="h-4 w-4 text-blue-600" />
                      <span>Quản lý công thức</span>
                    </DropdownMenuItem>
                  )}
                  {user.role === "Admin" && (
                    <DropdownMenuItem onClick={() => router.push("/admin/users")}>
                      <Users className="h-4 w-4 text-purple-600" />
                      <span>Quản lý người dùng</span>
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={handleLogout} className="text-red-600 hover:bg-red-50 hover:text-red-700">
                    <LogOut className="h-4 w-4 text-red-600" />
                    <span>Đăng xuất</span>
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            ) : (
              /* Guest Auth Buttons */
              <div className="hidden sm:flex items-center gap-2">
                <Link
                  href="/login"
                  className="inline-flex h-8 items-center rounded-md px-3 text-xs text-gray-700 hover:bg-gray-100"
                >
                  Đăng nhập
                </Link>
                <Link
                  href="/register"
                  className="inline-flex h-8 items-center rounded-md bg-emerald-700 px-3 text-xs font-semibold text-white hover:bg-emerald-800"
                >
                  Đăng ký
                </Link>
              </div>
            )}

            {/* Mobile Hamburger Button */}
            <button
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="lg:hidden p-2 rounded-lg text-gray-600 hover:bg-gray-100 transition-colors"
              aria-label="Menu"
            >
              {mobileMenuOpen ? (
                <X className="h-6 w-6" />
              ) : (
                <Menu className="h-6 w-6" />
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Mobile Drawer Navigation */}
      {mobileMenuOpen && (
        <div className="lg:hidden border-t border-gray-100 bg-white px-4 pt-3 pb-6 space-y-3 shadow-lg animate-in slide-in-from-top-2">
          {/* Mobile Search */}
          <form onSubmit={handleSearchSubmit} className="relative w-full flex items-center">
            <input
              type="text"
              data-testid="mobile-search-input"
              placeholder="Tìm công thức, món ăn..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full h-10 pl-9 pr-20 text-sm rounded-lg border border-gray-200 bg-gray-50 focus:bg-white focus:border-emerald-600 outline-none"
            />
            <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400 pointer-events-none" />
            <button
              type="submit"
              data-testid="mobile-search-submit"
              className="absolute right-1.5 top-1/2 -translate-y-1/2 px-3 py-1.5 text-xs font-bold rounded-md text-white bg-emerald-700 hover:bg-emerald-800 transition-colors cursor-pointer"
            >
              Tìm kiếm
            </button>
          </form>

          {/* Mobile Nav Links */}
          <nav className="flex flex-col space-y-1">
            {navLinks.map((link) => {
              const isActive =
                pathname === link.href ||
                (link.href !== "/" && pathname.startsWith(link.href));
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  onClick={() => setMobileMenuOpen(false)}
                  className={`px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                    isActive
                      ? "bg-emerald-700 text-white font-semibold"
                      : "text-gray-700 hover:bg-emerald-50"
                  }`}
                >
                  {link.label}
                </Link>
              );
            })}
            {user.role === "Admin" && (
              <Link
                href="/admin/users"
                onClick={() => setMobileMenuOpen(false)}
                className={`rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${
                  pathname.startsWith("/admin/users")
                    ? "bg-purple-700 text-white"
                    : "text-purple-700 hover:bg-purple-50"
                }`}
              >
                Quản lý người dùng
              </Link>
            )}
          </nav>

          {user.role !== "Guest" && (
            <Link
              href="/profile"
              onClick={() => setMobileMenuOpen(false)}
              className="inline-flex items-center gap-2 rounded-lg px-3 py-2.5 text-sm font-medium text-gray-700 hover:bg-emerald-50"
            >
              <UserCircle className="h-4 w-4 text-emerald-700" />
              Hồ sơ cá nhân
            </Link>
          )}

          {hasAuthorAccess && (
            <Link
              href="/dashboard/recipes"
              onClick={() => setMobileMenuOpen(false)}
              className="inline-flex items-center gap-2 rounded-lg px-3 py-2.5 text-sm font-medium text-blue-700 hover:bg-blue-50"
            >
              <PenTool className="h-4 w-4" />
              Quản lý công thức
            </Link>
          )}

          {user.role === "Guest" && (
            <div className="grid grid-cols-2 gap-2 pt-2 border-t border-gray-100">
              <Link
                href="/login"
                onClick={() => setMobileMenuOpen(false)}
                className="inline-flex h-8 items-center justify-center rounded-md border border-gray-300 px-3 text-xs font-medium text-gray-700 hover:bg-gray-50"
              >
                Đăng nhập
              </Link>
              <Link
                href="/register"
                onClick={() => setMobileMenuOpen(false)}
                className="inline-flex h-8 items-center justify-center rounded-md bg-emerald-700 px-3 text-xs font-semibold text-white hover:bg-emerald-800"
              >
                Đăng ký
              </Link>
            </div>
          )}

          <div className="pt-3 border-t border-gray-100 flex items-center justify-between">
            <span className="text-xs text-gray-500 font-medium">
              Vai trò hiện tại: <strong>{user.role}</strong>
            </span>
          </div>
        </div>
      )}

    </header>
  );
}

export default Header;
