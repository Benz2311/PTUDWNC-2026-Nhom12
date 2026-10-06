import Link from "next/link";

export default function Footer() {
  return (
    <footer className="bg-white border-t border-emerald-100 mt-16 text-gray-600 text-sm">
      <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          {/* Brand info */}
          <div className="md:col-span-2 space-y-3">
            <div className="flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-[#0d5c3a] text-white">
                <svg className="w-5 h-5 text-amber-400" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12 2a4.5 4.5 0 0 0-4.47 4.03A4.5 4.5 0 0 0 3 10.5a4.5 4.5 0 0 0 3.73 4.43A4 4 0 0 0 10 18h4a4 4 0 0 0 3.27-3.07A4.5 4.5 0 0 0 21 10.5a4.5 4.5 0 0 0-4.53-4.47A4.5 4.5 0 0 0 12 2zm-2 18v2h4v-2h-4z" />
                </svg>
              </div>
              <span className="text-lg font-bold text-[#0d5c3a]">Culinary Blog</span>
            </div>
            <p className="text-gray-500 text-xs leading-relaxed max-w-md">
              Nền tảng chia sẻ và học nấu ăn trực tuyến. Khám phá hàng ngàn công thức nấu ăn phong phú từ món Việt, món Á đến món Âu cùng hướng dẫn từng bước chi tiết và thông tin dinh dưỡng minh bạch.
            </p>
            <p className="text-xs text-emerald-800 font-medium italic">
              &quot;Nấu ăn là nghệ thuật và là một hành trình khám phá!&quot;
            </p>
          </div>

          {/* Quick links */}
          <div>
            <h4 className="text-xs font-bold text-gray-900 uppercase tracking-wider mb-3">Khám phá</h4>
            <ul className="space-y-2 text-xs">
              <li>
                <Link href="/recipes" className="hover:text-[#0d5c3a] transition-colors">
                  Tất cả công thức
                </Link>
              </li>
              <li>
                <Link href="/categories" className="hover:text-[#0d5c3a] transition-colors">
                  Danh mục món ăn
                </Link>
              </li>
              <li>
                <Link href="/recipes?difficulty=Easy" className="hover:text-[#0d5c3a] transition-colors">
                  Công thức cho người mới (Dễ)
                </Link>
              </li>
              <li>
                <Link href="/recipes?sort=cooktime" className="hover:text-[#0d5c3a] transition-colors">
                  Món ngon nấu nhanh
                </Link>
              </li>
            </ul>
          </div>

          {/* Module TV2 Quick access */}
          <div>
            <h4 className="text-xs font-bold text-gray-900 uppercase tracking-wider mb-3">Phân hệ TV2</h4>
            <ul className="space-y-2 text-xs">
              <li>
                <Link href="/dashboard/statistics" className="hover:text-[#0d5c3a] transition-colors">
                  Báo cáo thống kê
                </Link>
              </li>
              <li>
                <Link href="/dashboard/categories" className="hover:text-[#0d5c3a] transition-colors">
                  Quản lý danh mục
                </Link>
              </li>
              <li>
                <Link href="/admin/recipes/trash" className="hover:text-[#0d5c3a] transition-colors">
                  Thùng rác công thức (Admin)
                </Link>
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-8 border-t border-gray-100 pt-6 flex flex-col sm:flex-row justify-between items-center text-xs text-gray-400">
          <p>© 2026 Culinary Blog - PTUDWNC Nhóm 12. Thành viên phụ trách: Lê Thị Ánh Nhung (2312709).</p>
          <p className="mt-2 sm:mt-0">Clean Architecture + Next.js App Router + Tailwind CSS</p>
        </div>
      </div>
    </footer>
  );
}
