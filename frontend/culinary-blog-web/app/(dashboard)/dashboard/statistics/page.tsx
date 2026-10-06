import { getCategoryStatistics } from "@/lib/api/categories";
import CategoryStatistics from "@/components/category/CategoryStatistics";
import Link from "next/link";

export const dynamic = "force-dynamic";

export default async function StatisticsDashboardPage() {
  const statistics = await getCategoryStatistics();

  return (
    <main className="min-h-screen bg-gray-50/60 py-10 px-4 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-6xl space-y-8">
        {/* Header (Screen 14 in Design) */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 sm:p-8 rounded-3xl border border-gray-100 shadow-xs">
          <div>
            <div className="flex items-center gap-2">
              <span className="h-2 w-2 rounded-full bg-emerald-600" />
              <span className="text-xs font-bold text-[#0d5c3a] uppercase tracking-wider">
                Báo cáo & Thống kê hệ thống
              </span>
            </div>
            <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 mt-1">
              Thống kê tổng quan hoạt động
            </h1>
            <p className="text-xs sm:text-sm text-gray-500">
              Tổng quan danh mục, vòng đời công thức món ăn và phân bố ẩm thực.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <Link
              href="/dashboard/categories"
              className="px-4 py-2 rounded-xl bg-gray-100 hover:bg-gray-200 text-xs font-semibold text-gray-700 transition-colors"
            >
              Quản lý danh mục
            </Link>
            <Link
              href="/admin/recipes/trash"
              className="px-4 py-2 rounded-xl bg-purple-50 text-purple-700 hover:bg-purple-100 text-xs font-semibold transition-colors"
            >
              Thùng rác (Admin)
            </Link>
          </div>
        </div>

        {/* Real Data Statistics Components */}
        <CategoryStatistics data={statistics} />
      </div>
    </main>
  );
}
