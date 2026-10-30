import React from "react";
import Link from "next/link";
import { ChefHat, Heart, MapPin } from "lucide-react";

export function Footer() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="w-full border-t border-emerald-100 bg-white text-gray-600 text-sm mt-auto">
      <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-8">
          {/* Brand Info */}
          <div className="lg:col-span-2 space-y-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-800 text-white shadow-sm">
                <ChefHat className="h-6 w-6 text-amber-400" />
              </div>
              <div>
                <span className="text-xl font-extrabold tracking-tight text-emerald-900">
                  Culinary<span className="text-amber-500">Blog</span>
                </span>
                <span className="block text-[11px] font-semibold text-emerald-700">
                  PTUDWNC 2026 - Nhóm 12
                </span>
              </div>
            </div>

            <p className="text-xs text-gray-500 leading-relaxed max-w-sm">
              Nền tảng chia sẻ và học nấu ăn trực tuyến hàng đầu. Khám phá hàng ngàn công thức nấu ăn phong phú từ món Việt, món Á đến món Âu với hướng dẫn từng bước chi tiết và định lượng chuẩn xác.
            </p>

            <div className="flex items-center gap-3 text-xs text-gray-500 pt-1">
              <div className="flex items-center gap-1">
                <MapPin className="h-3.5 w-3.5 text-emerald-700" />
                <span>Khoa CNTT - Đại học Đà Lạt</span>
              </div>
            </div>
          </div>

          {/* Quick Links */}
          <div>
            <h4 className="text-xs font-bold text-gray-900 uppercase tracking-wider mb-4 border-l-2 border-emerald-600 pl-2">
              Khám phá
            </h4>
            <ul className="space-y-2.5 text-xs">
              <li>
                <Link href="/" className="hover:text-emerald-700 transition-colors">
                  Trang chủ
                </Link>
              </li>
              <li>
                <Link href="/recipes" className="hover:text-emerald-700 transition-colors">
                  Tất cả công thức
                </Link>
              </li>
              <li>
                <Link href="/categories" className="hover:text-emerald-700 transition-colors">
                  Danh mục ẩm thực
                </Link>
              </li>
              <li>
                <Link href="/recipes?sort=popular" className="hover:text-emerald-700 transition-colors">
                  Món ăn nổi bật
                </Link>
              </li>
              <li>
                <Link href="/recipes?difficulty=Easy" className="hover:text-emerald-700 transition-colors">
                  Món ngon dễ làm
                </Link>
              </li>
            </ul>
          </div>

          {/* Project & Group Info */}
          <div>
            <h4 className="text-xs font-bold text-gray-900 uppercase tracking-wider mb-4 border-l-2 border-emerald-600 pl-2">
              Thông tin nhóm 12
            </h4>
            <ul className="space-y-2 text-xs text-gray-500">
              <li>
                <span className="font-semibold text-gray-700">Học phần:</span> Phát triển ứng dụng Web nâng cao
              </li>
              <li>
                <span className="font-semibold text-gray-700">Thành viên phụ trách UI:</span>
                <br />
                <span className="text-emerald-800 font-medium">Lê Thị Ánh Nhung (2312709)</span>
              </li>
              <li>
                <span className="font-semibold text-gray-700">Kiến trúc hệ thống:</span>
                <br />
                Clean Architecture + Next.js 15
              </li>
            </ul>
          </div>

          {/* Dashboard & Admin Quick Links */}
          <div>
            <h4 className="text-xs font-bold text-gray-900 uppercase tracking-wider mb-4 border-l-2 border-emerald-600 pl-2">
              Quản trị & Tác giả
            </h4>
            <ul className="space-y-2.5 text-xs">
              <li>
                <Link href="/dashboard/categories" className="hover:text-emerald-700 transition-colors">
                  Quản lý danh mục
                </Link>
              </li>
              <li>
                <Link href="/admin/users" className="hover:text-emerald-700 transition-colors">
                  Quản lý người dùng
                </Link>
              </li>
              <li>
                <Link href="/recipes/create" className="hover:text-emerald-700 transition-colors">
                  Tạo công thức mới
                </Link>
              </li>
              <li>
                <a
                  href="https://github.com/Benz2311/PTUDWNC-2026-Nhom12"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 text-gray-600 hover:text-emerald-700 transition-colors"
                >
                  <svg className="h-3.5 w-3.5 fill-current" viewBox="0 0 24 24">
                    <path fillRule="evenodd" clipRule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.53 1.032 1.53 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" />
                  </svg>
                  <span>GitHub Repository</span>
                </a>
              </li>
            </ul>
          </div>
        </div>

        {/* Bottom copyright */}
        <div className="mt-10 border-t border-gray-100 pt-6 flex flex-col sm:flex-row justify-between items-center text-xs text-gray-400 gap-2">
          <p className="flex items-center gap-1 text-center sm:text-left">
            © {currentYear} Culinary Blog. Bản quyền thuộc về Nhóm 12 - PTUDWNC. Xây dựng với{" "}
            <Heart className="h-3.5 w-3.5 text-red-500 fill-red-500 inline" /> vì tình yêu ẩm thực.
          </p>
          <div className="flex items-center gap-4 text-gray-500">
            <span className="text-[11px]">Next.js 15 App Router • Tailwind CSS</span>
          </div>
        </div>
      </div>
    </footer>
  );
}

export default Footer;
