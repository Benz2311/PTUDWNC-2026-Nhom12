"use client";

import type { CategoryStatistics as CategoryStatsType } from "@/types/category";

interface CategoryStatisticsProps {
  data: CategoryStatsType;
}

export default function CategoryStatistics({ data }: CategoryStatisticsProps) {
  const {
    totalCategories,
    totalRecipes,
    publishedRecipes,
    draftRecipes,
    archivedRecipes = 0,
    categories = [],
    topCategories = [],
    recipesByMonth = [],
  } = data;

  // Use topCategories from API or sort categories by recipeCount
  const displayTopCategories =
    topCategories.length > 0
      ? topCategories
      : [...categories].sort((a, b) => b.recipeCount - a.recipeCount).slice(0, 5);

  // Calculate maximum monthly count for SVG chart scaling
  const maxMonthlyCount =
    recipesByMonth.length > 0
      ? Math.max(...recipesByMonth.map((m) => m.count), 1)
      : 1;

  return (
    <div className="space-y-8">
      {/* ── 1. SUMMARY CARDS ROW (Screen 14 in Design) ───────────────── */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4">
        {/* Total Categories */}
        <div className="rounded-3xl bg-white p-5 border border-gray-100 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-gray-500">
              Tổng danh mục
            </span>
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-50 text-[#0d5c3a]">
              📁
            </div>
          </div>
          <div className="mt-4">
            <span className="text-2xl sm:text-3xl font-extrabold text-gray-900 block">
              {totalCategories}
            </span>
            <span className="text-[11px] text-gray-400 font-medium">Danh mục hoạt động</span>
          </div>
        </div>

        {/* Total Recipes */}
        <div className="rounded-3xl bg-white p-5 border border-gray-100 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-gray-500">
              Tổng công thức
            </span>
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-50 text-[#0d5c3a]">
              🍲
            </div>
          </div>
          <div className="mt-4">
            <span className="text-2xl sm:text-3xl font-extrabold text-[#0d5c3a] block">
              {totalRecipes}
            </span>
            <span className="text-[11px] text-gray-400 font-medium">Toàn bộ món ăn</span>
          </div>
        </div>

        {/* Published Recipes */}
        <div className="rounded-3xl bg-white p-5 border border-gray-100 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-gray-500">
              Đã xuất bản
            </span>
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-100 text-emerald-800">
              ✅
            </div>
          </div>
          <div className="mt-4">
            <span className="text-2xl sm:text-3xl font-extrabold text-emerald-700 block">
              {publishedRecipes}
            </span>
            <span className="text-[11px] text-gray-400 font-medium">
              {totalRecipes > 0 ? `${Math.round((publishedRecipes / totalRecipes) * 100)}% tổng số` : "0%"}
            </span>
          </div>
        </div>

        {/* Draft Recipes */}
        <div className="rounded-3xl bg-white p-5 border border-gray-100 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-gray-500">
              Bản nháp
            </span>
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-slate-100 text-slate-700">
              📝
            </div>
          </div>
          <div className="mt-4">
            <span className="text-2xl sm:text-3xl font-extrabold text-slate-700 block">
              {draftRecipes}
            </span>
            <span className="text-[11px] text-gray-400 font-medium">Đang soạn thảo</span>
          </div>
        </div>

        {/* Archived Recipes */}
        <div className="rounded-3xl bg-white p-5 border border-gray-100 shadow-xs flex flex-col justify-between col-span-2 sm:col-span-1">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-gray-500">
              Đã lưu trữ
            </span>
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-amber-50 text-amber-700">
              📦
            </div>
          </div>
          <div className="mt-4">
            <span className="text-2xl sm:text-3xl font-extrabold text-amber-700 block">
              {archivedRecipes}
            </span>
            <span className="text-[11px] text-gray-400 font-medium">Ẩn khỏi danh sách</span>
          </div>
        </div>
      </div>

      {/* ── 2. CHARTS ROW (Screen 14 in Design) ────────────────────────── */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left 2 Cols: Recipe Mới Theo Thời Gian (SVG Chart) */}
        <div className="lg:col-span-2 rounded-3xl bg-white p-6 sm:p-8 border border-gray-100 shadow-xs space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-base font-bold text-gray-900">
                Công thức mới theo thời gian
              </h3>
              <p className="text-xs text-gray-500">
                Số lượng công thức được tạo theo từng tháng (Dữ liệu API thực)
              </p>
            </div>
            <span className="rounded-full bg-emerald-50 text-[#0d5c3a] px-3 py-1 text-xs font-bold">
              Biểu đồ trực quan
            </span>
          </div>

          {recipesByMonth.length === 0 ? (
            <div className="h-64 flex flex-col items-center justify-center text-gray-400 text-xs italic">
              Chưa có dữ liệu thống kê theo tháng.
            </div>
          ) : (
            <div className="space-y-4">
              {/* SVG Line / Bar Chart */}
              <div className="h-60 w-full relative flex items-end gap-3 pt-6 border-b border-gray-100 pb-2">
                {recipesByMonth.map((item, idx) => {
                  const heightPercent = Math.max(12, Math.round((item.count / maxMonthlyCount) * 100));
                  return (
                    <div
                      key={`${item.year}-${item.month}`}
                      className="flex-1 flex flex-col items-center justify-end h-full group"
                    >
                      {/* Count tooltip */}
                      <span className="mb-2 text-[11px] font-bold text-[#0d5c3a] opacity-0 group-hover:opacity-100 transition-opacity">
                        {item.count} món
                      </span>
                      {/* Bar with gradient */}
                      <div
                        style={{ height: `${heightPercent}%` }}
                        className="w-full max-w-[42px] rounded-t-xl bg-gradient-to-t from-[#0d5c3a] to-emerald-400 group-hover:from-emerald-700 group-hover:to-emerald-300 transition-all shadow-xs"
                      />
                      {/* Month label */}
                      <span className="mt-2 text-[10px] font-medium text-gray-500 truncate">
                        T{item.month}/{item.year.toString().slice(-2)}
                      </span>
                    </div>
                  );
                })}
              </div>

              <div className="flex items-center justify-between text-xs text-gray-400">
                <span>Trục ngang: Tháng / Năm</span>
                <span>Trục đứng: Số lượng công thức</span>
              </div>
            </div>
          )}
        </div>

        {/* Right 1 Col: Top Categories */}
        <div className="rounded-3xl bg-white p-6 sm:p-8 border border-gray-100 shadow-xs space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-base font-bold text-gray-900">Top danh mục</h3>
              <p className="text-xs text-gray-500">Nhiều công thức nhất</p>
            </div>
            <span className="text-base">🏆</span>
          </div>

          <div className="space-y-4">
            {displayTopCategories.map((item, index) => {
              const pct =
                item.percentage !== undefined
                  ? item.percentage
                  : totalRecipes > 0
                  ? Math.round((item.recipeCount / totalRecipes) * 100)
                  : 0;

              return (
                <div key={item.categoryId} className="space-y-1.5">
                  <div className="flex items-center justify-between text-xs">
                    <div className="flex items-center gap-2">
                      <span className={`flex h-5 w-5 items-center justify-center rounded-full text-[10px] font-bold ${
                        index === 0
                          ? "bg-amber-100 text-amber-800"
                          : index === 1
                          ? "bg-gray-200 text-gray-700"
                          : "bg-emerald-50 text-[#0d5c3a]"
                      }`}>
                        {index + 1}
                      </span>
                      <span className="font-semibold text-gray-800">{item.categoryName}</span>
                    </div>
                    <span className="font-bold text-[#0d5c3a]">{item.recipeCount} món ({pct}%)</span>
                  </div>

                  {/* Progress bar */}
                  <div className="h-2 w-full rounded-full bg-gray-100 overflow-hidden">
                    <div
                      style={{ width: `${Math.min(100, Math.max(5, pct))}%` }}
                      className="h-full rounded-full bg-[#0d5c3a]"
                    />
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* ── 3. FULL CATEGORY RATIO TABLE ───────────────────────────────── */}
      <div className="rounded-3xl bg-white p-6 sm:p-8 border border-gray-100 shadow-xs space-y-4">
        <div className="flex items-center justify-between pb-3 border-b border-gray-100">
          <div>
            <h3 className="text-base font-bold text-gray-900">
              Tỷ lệ phân bố công thức theo danh mục
            </h3>
            <p className="text-xs text-gray-500">
              Chi tiết số lượng và tỷ trọng của toàn bộ {categories.length} danh mục
            </p>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-gray-600">
            <thead className="bg-gray-50 text-[11px] font-bold uppercase tracking-wider text-gray-500">
              <tr>
                <th className="py-3 px-4">STT</th>
                <th className="py-3 px-6">Tên danh mục</th>
                <th className="py-3 px-4 text-center">Số công thức</th>
                <th className="py-3 px-6">Tỷ lệ đóng góp</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {categories.map((cat, idx) => {
                const ratio =
                  cat.percentage !== undefined
                    ? cat.percentage
                    : totalRecipes > 0
                    ? Math.round((cat.recipeCount / totalRecipes) * 100)
                    : 0;

                return (
                  <tr key={cat.categoryId} className="hover:bg-emerald-50/20">
                    <td className="py-3 px-4 font-mono text-gray-400">{idx + 1}</td>
                    <td className="py-3 px-6 font-bold text-gray-900">{cat.categoryName}</td>
                    <td className="py-3 px-4 text-center font-bold text-[#0d5c3a]">
                      {cat.recipeCount}
                    </td>
                    <td className="py-3 px-6">
                      <div className="flex items-center gap-3">
                        <div className="h-2 w-36 rounded-full bg-gray-100 overflow-hidden">
                          <div
                            style={{ width: `${ratio}%` }}
                            className="h-full rounded-full bg-[#0d5c3a]"
                          />
                        </div>
                        <span className="font-semibold text-gray-700">{ratio}%</span>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
