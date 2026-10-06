"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState, useEffect } from "react";
import { getStoredToken, setStoredToken, removeStoredToken } from "@/lib/api/client";

export default function Navbar() {
  const pathname = usePathname();
  const [tokenInput, setTokenInput] = useState("");
  const [currentRole, setCurrentRole] = useState<"Guest" | "Author" | "Admin">("Guest");
  const [showAuthModal, setShowAuthModal] = useState(false);

  useEffect(() => {
    const saved = getStoredToken();
    if (saved) {
      setTokenInput(saved);
      // Infer simple role from token payload if jwt
      try {
        const payload = JSON.parse(atob(saved.split(".")[1]));
        if (payload.role?.includes("Admin") || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] === "Admin") {
          setCurrentRole("Admin");
        } else if (payload.role?.includes("Author") || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] === "Author") {
          setCurrentRole("Author");
        } else {
          setCurrentRole("Author");
        }
      } catch {
        setCurrentRole("Author");
      }
    } else {
      setCurrentRole("Guest");
    }
  }, []);

  const handleSaveToken = (token: string, role: "Guest" | "Author" | "Admin") => {
    if (role === "Guest" || !token.trim()) {
      removeStoredToken();
      setTokenInput("");
      setCurrentRole("Guest");
    } else {
      setStoredToken(token.trim());
      setTokenInput(token.trim());
      setCurrentRole(role);
    }
    setShowAuthModal(false);
    window.location.reload();
  };

  const navLinks = [
    { href: "/", label: "Trang chủ" },
    { href: "/recipes", label: "Khám phá món ăn" },
    { href: "/categories", label: "Danh mục" },
    { href: "/dashboard/statistics", label: "Thống kê" },
    { href: "/dashboard/categories", label: "Quản lý danh mục" },
    { href: "/admin/recipes/trash", label: "Thùng rác (Admin)" },
  ];

  return (
    <header className="sticky top-0 z-50 bg-white/95 backdrop-blur-md border-b border-emerald-100 shadow-xs">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between gap-4">
          {/* Brand Logo */}
          <Link href="/" className="flex items-center gap-3 group">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-[#0d5c3a] text-white shadow-xs group-hover:scale-105 transition-transform">
              {/* Chef Hat Icon */}
              <svg className="w-6 h-6 text-amber-400" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2a4.5 4.5 0 0 0-4.47 4.03A4.5 4.5 0 0 0 3 10.5a4.5 4.5 0 0 0 3.73 4.43A4 4 0 0 0 10 18h4a4 4 0 0 0 3.27-3.07A4.5 4.5 0 0 0 21 10.5a4.5 4.5 0 0 0-4.53-4.47A4.5 4.5 0 0 0 12 2zm-2 18v2h4v-2h-4z" />
              </svg>
            </div>
            <div>
              <div className="flex items-center gap-1.5">
                <span className="text-xl font-bold tracking-tight text-[#0d5c3a]">Culinary Blog</span>
                <span className="hidden sm:inline-block rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-[#0d5c3a]">
                  TV2
                </span>
              </div>
              <p className="text-[11px] text-gray-500 font-medium hidden sm:block">
                Hành trình trải nghiệm ứng dụng ẩm thực
              </p>
            </div>
          </Link>

          {/* Desktop Navigation Links */}
          <nav className="hidden lg:flex items-center gap-1">
            {navLinks.map((link) => {
              const isActive = pathname === link.href || (link.href !== "/" && pathname.startsWith(link.href));
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  className={`px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                    isActive
                      ? "bg-[#0d5c3a] text-white font-semibold shadow-xs"
                      : "text-gray-700 hover:text-[#0d5c3a] hover:bg-emerald-50"
                  }`}
                >
                  {link.label}
                </Link>
              );
            })}
          </nav>

          {/* Right Action / Role Switcher */}
          <div className="flex items-center gap-2">
            <button
              onClick={() => setShowAuthModal(true)}
              className="flex items-center gap-2 px-3 py-1.5 text-xs font-medium rounded-full border border-emerald-200 bg-emerald-50/80 text-[#0d5c3a] hover:bg-emerald-100 transition-colors cursor-pointer"
              title="Cấu hình Token / Vai trò để test API"
            >
              <span className={`h-2 w-2 rounded-full ${currentRole === "Admin" ? "bg-purple-600" : currentRole === "Author" ? "bg-emerald-600" : "bg-gray-400"}`} />
              <span>Vai trò: <strong className="font-semibold">{currentRole}</strong></span>
              <svg className="w-3.5 h-3.5 text-emerald-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>
          </div>
        </div>

        {/* Mobile Navigation bar */}
        <div className="lg:hidden flex overflow-x-auto py-2 gap-1 border-t border-gray-100 no-scrollbar">
          {navLinks.map((link) => {
            const isActive = pathname === link.href || (link.href !== "/" && pathname.startsWith(link.href));
            return (
              <Link
                key={link.href}
                href={link.href}
                className={`whitespace-nowrap px-3 py-1 text-xs rounded-full font-medium ${
                  isActive ? "bg-[#0d5c3a] text-white" : "text-gray-600 hover:bg-emerald-50"
                }`}
              >
                {link.label}
              </Link>
            );
          })}
        </div>
      </div>

      {/* Role & Token Configuration Modal */}
      {showAuthModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl border border-gray-100">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <h3 className="text-lg font-bold text-gray-900">Thiết lập Xác thực (JWT Auth)</h3>
              <button
                onClick={() => setShowAuthModal(false)}
                className="text-gray-400 hover:text-gray-600 p-1 rounded-lg"
              >
                ✕
              </button>
            </div>

            <p className="mt-2 text-xs text-gray-600">
              Chọn vai trò hoặc dán Bearer Token để kiểm thử các tính năng phân quyền: xem công thức của mình (<code>mine=true</code>), lọc <code>authorId</code> (Admin), tạo/sửa/xóa danh mục, và xem thùng rác (Admin).
            </p>

            <div className="mt-4 space-y-3">
              <div>
                <label className="block text-xs font-semibold text-gray-700 mb-1">
                  Chọn chế độ nhanh:
                </label>
                <div className="grid grid-cols-3 gap-2">
                  <button
                    type="button"
                    onClick={() => handleSaveToken("", "Guest")}
                    className={`py-2 px-3 text-xs font-medium rounded-lg border text-center transition-all ${
                      currentRole === "Guest"
                        ? "border-[#0d5c3a] bg-emerald-50 text-[#0d5c3a] font-bold"
                        : "border-gray-200 text-gray-700 hover:bg-gray-50"
                    }`}
                  >
                    👤 Khách (Guest)
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      // Demo Author JWT placeholder
                      handleSaveToken("mock_author_jwt", "Author");
                    }}
                    className={`py-2 px-3 text-xs font-medium rounded-lg border text-center transition-all ${
                      currentRole === "Author"
                        ? "border-[#0d5c3a] bg-emerald-50 text-[#0d5c3a] font-bold"
                        : "border-gray-200 text-gray-700 hover:bg-gray-50"
                    }`}
                  >
                    ✍️ Tác giả (Author)
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      // Demo Admin JWT placeholder
                      handleSaveToken("mock_admin_jwt", "Admin");
                    }}
                    className={`py-2 px-3 text-xs font-medium rounded-lg border text-center transition-all ${
                      currentRole === "Admin"
                        ? "border-purple-600 bg-purple-50 text-purple-700 font-bold"
                        : "border-gray-200 text-gray-700 hover:bg-gray-50"
                    }`}
                  >
                    🛡️ Quản trị (Admin)
                  </button>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-700 mb-1">
                  Hoặc dán Access Token thực tế từ backend:
                </label>
                <textarea
                  rows={3}
                  value={tokenInput}
                  onChange={(e) => setTokenInput(e.target.value)}
                  placeholder="Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                  className="w-full rounded-lg border border-gray-300 p-2 text-xs font-mono focus:border-[#0d5c3a] focus:ring-1 focus:ring-[#0d5c3a] outline-none"
                />
              </div>
            </div>

            <div className="mt-5 flex justify-end gap-2">
              <button
                type="button"
                onClick={() => handleSaveToken("", "Guest")}
                className="px-3 py-1.5 text-xs font-medium text-gray-600 hover:bg-gray-100 rounded-lg"
              >
                Xóa Token (Về Guest)
              </button>
              <button
                type="button"
                onClick={() => handleSaveToken(tokenInput, currentRole)}
                className="px-4 py-1.5 text-xs font-semibold text-white bg-[#0d5c3a] hover:bg-[#094229] rounded-lg shadow-xs"
              >
                Lưu và Áp dụng
              </button>
            </div>
          </div>
        </div>
      )}
    </header>
  );
}
