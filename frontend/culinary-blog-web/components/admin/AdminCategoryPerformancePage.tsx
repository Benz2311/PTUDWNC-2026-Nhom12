"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import {
  PieChart,
  TrendingUp,
  AlertTriangle,
  ArrowRight,
  Eye,
  ArrowUpRight,
  ArrowDownRight,
} from "lucide-react";
import type { CategoryPerformanceAnalyticsResponse } from "@/types/analytics";
import { getCategoryPerformanceAnalytics } from "@/lib/api/analytics";
import { ErrorState } from "@/components/common/ErrorState";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";

export function AdminCategoryPerformancePage() {
  const [days, setDays] = useState<number>(7);
  const [data, setData] = useState<CategoryPerformanceAnalyticsResponse | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [sortBy, setSortBy] = useState<"growth" | "views" | "new" | "published">("growth");

  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await getCategoryPerformanceAnalytics(days);
      setData(res);
    } catch (err: any) {
      console.error("Error fetching category performance analytics:", err);
      setError("Không thể tải báo cáo hiệu quả danh mục. Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [days]);

  return (
    <div className="space-y-8" data-testid="admin-category-performance">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-gray-100">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-100 text-emerald-800 text-xs font-bold mb-2">
            <PieChart className="h-3.5 w-3.5 text-emerald-700" />
            <span>Category Performance Analytics</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
            Hiệu quả danh mục & Tăng trưởng nội dung
          </h1>
          <p className="text-xs sm:text-sm text-gray-600 mt-1">
            Phân tích tốc độ tăng trưởng công thức, tương tác lượt xem và độ tích cực theo từng danh mục ẩm thực.
          </p>
        </div>

        {/* Days Filter */}
        <div className="inline-flex items-center p-1 rounded-xl bg-gray-100 text-xs font-bold self-start sm:self-auto">
          <button
            type="button"
            onClick={() => setDays(7)}
            className={`px-4 py-2 rounded-lg transition-all cursor-pointer ${
              days === 7
                ? "bg-white text-emerald-800 shadow-xs"
                : "text-gray-600 hover:text-gray-900"
            }`}
          >
            7 ngày qua
          </button>
          <button
            type="button"
            onClick={() => setDays(30)}
            className={`px-4 py-2 rounded-lg transition-all cursor-pointer ${
              days === 30
                ? "bg-white text-emerald-800 shadow-xs"
                : "text-gray-600 hover:text-gray-900"
            }`}
          >
            30 ngày qua
          </button>
        </div>
      </div>

      {loading ? (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="h-64 bg-gray-100 rounded-3xl animate-pulse" />
            <div className="h-64 bg-gray-100 rounded-3xl animate-pulse" />
          </div>
          <div className="h-96 bg-gray-100 rounded-3xl animate-pulse" />
        </div>
      ) : error ? (
        <ErrorState
          title="Không thể tải báo cáo hiệu quả danh mục"
          message={error}
          onRetry={fetchData}
        />
      ) : !data ? null : (
        <div className="space-y-8">
          {/* Highlight Cards: Fastest Growing vs Low Activity */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Fastest Growing */}
            <Card className="p-6 rounded-3xl border border-emerald-100 bg-linear-to-br from-emerald-50/50 to-white shadow-xs">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-2 text-emerald-800 font-bold text-sm">
                  <TrendingUp className="h-4 w-4" />
                  <span>Danh mục tăng trưởng nhanh nhất ({days} ngày)</span>
                </div>
                <Badge className="bg-emerald-100 text-emerald-800 font-bold text-[10px]">
                  Tăng trưởng cao
                </Badge>
              </div>

              {data.fastestGrowingCategories.length === 0 ? (
                <p className="text-xs text-gray-400 py-6 text-center">
                  Chưa có danh mục tăng trưởng đột phá trong kỳ.
                </p>
              ) : (
                <div className="space-y-3">
                  {data.fastestGrowingCategories.map((item, idx) => (
                    <div
                      key={item.categoryId}
                      className="flex items-center justify-between p-3 rounded-2xl bg-white border border-gray-100 shadow-2xs"
                    >
                      <div className="flex items-center gap-3">
                        <span className="h-6 w-6 rounded-full bg-emerald-100 text-emerald-800 flex items-center justify-center font-extrabold text-xs">
                          {idx + 1}
                        </span>
                        <div>
                          <Link
                            href={`/categories/${item.slug}`}
                            className="font-bold text-gray-900 text-xs hover:text-emerald-700 transition-colors"
                          >
                            {item.categoryName}
                          </Link>
                          <div className="flex items-center gap-2 text-[10px] text-gray-400 mt-0.5">
                            <span>+{item.publishedRecipesCount} món xuất bản</span>
                            <span>•</span>
                            <span>{item.totalViewsInPeriod} lượt xem</span>
                          </div>
                        </div>
                      </div>

                      <span className="inline-flex items-center text-xs font-extrabold text-emerald-700 bg-emerald-50 px-2.5 py-1 rounded-full">
                        <ArrowUpRight className="h-3 w-3 mr-0.5" />
                        {item.growthRate > 0 ? `+${item.growthRate}%` : `${item.growthRate}%`}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </Card>

            {/* Low Activity Categories */}
            <Card className="p-6 rounded-3xl border border-amber-100 bg-linear-to-br from-amber-50/40 to-white shadow-xs">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-2 text-amber-800 font-bold text-sm">
                  <AlertTriangle className="h-4 w-4" />
                  <span>Danh mục ít phát sinh nội dung mới</span>
                </div>
                <Badge className="bg-amber-100 text-amber-800 font-bold text-[10px]">
                  Cần bổ sung
                </Badge>
              </div>

              {data.lowActivityCategories.length === 0 ? (
                <p className="text-xs text-gray-400 py-6 text-center">
                  Tất cả các danh mục đều phát sinh công thức mới đều đặn.
                </p>
              ) : (
                <div className="space-y-3">
                  {data.lowActivityCategories.map((item) => (
                    <div
                      key={item.categoryId}
                      className="flex items-center justify-between p-3 rounded-2xl bg-white border border-gray-100 shadow-2xs"
                    >
                      <div>
                        <span className="font-bold text-gray-900 text-xs">
                          {item.categoryName}
                        </span>
                        <div className="text-[10px] text-gray-400 mt-0.5">
                          {item.newRecipesCount} món tạo mới • {item.publishedRecipesCount} món xuất bản
                        </div>
                      </div>

                      <Link
                        href="/admin/categories"
                        className="inline-flex items-center gap-1 text-[11px] font-bold text-emerald-700 hover:text-emerald-800"
                      >
                        <span>Quản lý</span>
                        <ArrowRight className="h-3 w-3" />
                      </Link>
                    </div>
                  ))}
                </div>
              )}
            </Card>
          </div>

          {/* Comparative Category Activity Chart */}
          <Card className="p-6 sm:p-8 rounded-3xl border border-gray-100 bg-white shadow-xs">
            <div className="mb-6">
              <h3 className="text-lg font-bold text-gray-900">
                So sánh mức độ quan tâm & công thức giữa các danh mục
              </h3>
              <p className="text-xs text-gray-500 mt-1">
                Tỷ trọng lượt xem và số lượng công thức xuất bản theo từng danh mục trong {days} ngày qua
              </p>
            </div>

            <div className="space-y-4">
              {(() => {
                const maxViews = Math.max(...data.categories.map((c) => c.totalViewsInPeriod), 1);

                return data.categories.map((cat) => {
                  const percentage = Math.round((cat.totalViewsInPeriod / maxViews) * 100);

                  return (
                    <div key={cat.categoryId} className="space-y-1.5">
                      <div className="flex items-center justify-between text-xs">
                        <span className="font-bold text-gray-800">
                          {cat.categoryName}
                        </span>
                        <div className="flex items-center gap-3 text-gray-500 text-[11px]">
                          <span>{cat.publishedRecipesCount} món xuất bản</span>
                          <span className="font-bold text-emerald-700 flex items-center gap-1">
                            <Eye className="h-3 w-3" /> {cat.totalViewsInPeriod} lượt
                          </span>
                        </div>
                      </div>

                      {/* Progress Bar */}
                      <div className="h-3 w-full bg-gray-100 rounded-full overflow-hidden">
                        <div
                          style={{ width: `${Math.max(percentage, 2)}%` }}
                          className="h-full bg-linear-to-r from-emerald-500 to-teal-600 rounded-full transition-all duration-500"
                        />
                      </div>
                    </div>
                  );
                });
              })()}
            </div>
          </Card>

          {/* Full Detailed Performance Table */}
          <Card className="rounded-3xl border border-gray-100 bg-white overflow-hidden shadow-xs">
            <div className="p-6 border-b border-gray-100 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div>
                <h3 className="text-lg font-bold text-gray-900">
                  Bảng chi tiết hiệu quả từng danh mục
                </h3>
                <p className="text-xs text-gray-500 mt-1">
                  Có thể sắp xếp linh hoạt và điều hướng đến trang quản lý Category
                </p>
              </div>

              {/* Sorting Filter */}
              <div className="flex items-center gap-2 text-xs">
                <span className="text-gray-500 font-medium">Sắp xếp theo:</span>
                <select
                  value={sortBy}
                  onChange={(e) => setSortBy(e.target.value as any)}
                  className="rounded-xl border border-gray-200 bg-gray-50 px-3 py-1.5 text-xs font-semibold text-gray-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                >
                  <option value="growth">Tăng trưởng cao nhất</option>
                  <option value="views">Lượt xem nhiều nhất</option>
                  <option value="published">Công thức xuất bản</option>
                  <option value="new">Công thức mới tạo</option>
                </select>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="bg-gray-50 text-gray-500 font-bold border-b border-gray-100">
                  <tr>
                    <th className="py-3.5 px-6">Danh mục</th>
                    <th className="py-3.5 px-6 text-center">Tạo mới ({days}d)</th>
                    <th className="py-3.5 px-6 text-center">Xuất bản ({days}d)</th>
                    <th className="py-3.5 px-6 text-center">Kỳ trước ({days}d)</th>
                    <th className="py-3.5 px-6 text-center">Tăng trưởng</th>
                    <th className="py-3.5 px-6 text-center">Lượt xem</th>
                    <th className="py-3.5 px-6 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {(() => {
                    const sorted = [...data.categories].sort((a, b) => {
                      if (sortBy === "growth") return b.growthRate - a.growthRate;
                      if (sortBy === "views") return b.totalViewsInPeriod - a.totalViewsInPeriod;
                      if (sortBy === "published") return b.publishedRecipesCount - a.publishedRecipesCount;
                      return b.newRecipesCount - a.newRecipesCount;
                    });

                    return sorted.map((cat) => (
                      <tr key={cat.categoryId} className="hover:bg-gray-50/80 transition-colors">
                        <td className="py-4 px-6 font-bold text-gray-900">
                          <Link
                            href={`/categories/${cat.slug}`}
                            className="hover:text-emerald-700 transition-colors"
                          >
                            {cat.categoryName}
                          </Link>
                        </td>
                        <td className="py-4 px-6 text-center font-semibold text-blue-600">
                          {cat.newRecipesCount}
                        </td>
                        <td className="py-4 px-6 text-center font-bold text-emerald-700">
                          {cat.publishedRecipesCount}
                        </td>
                        <td className="py-4 px-6 text-center text-gray-400">
                          {cat.previousPublishedCount}
                        </td>
                        <td className="py-4 px-6 text-center">
                          <span
                            className={`inline-flex items-center text-xs font-bold px-2.5 py-0.5 rounded-full ${
                              cat.growthRate > 0
                                ? "bg-emerald-50 text-emerald-700"
                                : cat.growthRate < 0
                                ? "bg-rose-50 text-rose-700"
                                : "bg-gray-50 text-gray-600"
                            }`}
                          >
                            {cat.growthRate > 0 ? (
                              <ArrowUpRight className="h-3 w-3 mr-0.5" />
                            ) : cat.growthRate < 0 ? (
                              <ArrowDownRight className="h-3 w-3 mr-0.5" />
                            ) : null}
                            {cat.growthRate > 0 ? `+${cat.growthRate}%` : `${cat.growthRate}%`}
                          </span>
                        </td>
                        <td className="py-4 px-6 text-center font-bold text-teal-800">
                          {cat.totalViewsInPeriod}
                        </td>
                        <td className="py-4 px-6 text-right">
                          <Link
                            href="/admin/categories"
                            className="inline-flex items-center gap-1 text-xs font-bold text-emerald-700 hover:text-emerald-800"
                          >
                            <span>Quản lý</span>
                            <ArrowRight className="h-3.5 w-3.5" />
                          </Link>
                        </td>
                      </tr>
                    ));
                  })()}
                </tbody>
              </table>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}

export default AdminCategoryPerformancePage;
