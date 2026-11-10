"use client";

import React, { useEffect, useState } from "react";
import {
  TrendingUp,
  TrendingDown,
  ChefHat,
  BarChart3,
} from "lucide-react";
import type { ActivityAnalyticsResponse } from "@/types/analytics";
import { getActivityAnalytics } from "@/lib/api/analytics";
import { ErrorState } from "@/components/common/ErrorState";
import { Card } from "@/components/ui/Card";

export function AdminActivityAnalyticsPage() {
  const [days, setDays] = useState<number>(7);
  const [data, setData] = useState<ActivityAnalyticsResponse | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await getActivityAnalytics(days);
      setData(res);
    } catch (err: any) {
      console.error("Error fetching activity analytics:", err);
      setError("Không thể tải dữ liệu thống kê hoạt động. Vui lòng kiểm tra quyền Admin.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [days]);

  return (
    <div className="space-y-8" data-testid="admin-activity-analytics">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-gray-100">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-100 text-emerald-800 text-xs font-bold mb-2">
            <BarChart3 className="h-3.5 w-3.5 text-emerald-700" />
            <span>Admin Advanced Analytics</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
            Phân tích hoạt động đăng tải (Activity Analytics)
          </h1>
          <p className="text-xs sm:text-sm text-gray-600 mt-1">
            Theo dõi xu hướng tạo mới, xuất bản công thức và bảng xếp hạng tác giả tích cực nhất.
          </p>
        </div>

        {/* Time period filter button group */}
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
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-32 bg-gray-100 rounded-3xl animate-pulse" />
            ))}
          </div>
          <div className="h-80 bg-gray-100 rounded-3xl animate-pulse" />
        </div>
      ) : error ? (
        <ErrorState
          title="Không thể tải báo cáo hoạt động"
          message={error}
          onRetry={fetchData}
        />
      ) : !data ? null : (
        <div className="space-y-8">
          {/* Summary Metric Cards with Growth Comparisons */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {/* Created Current */}
            <Card className="p-6 rounded-3xl border border-gray-100 bg-white shadow-xs">
              <span className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                Công thức tạo mới ({days} ngày)
              </span>
              <div className="mt-2 flex items-baseline justify-between">
                <span className="text-3xl font-extrabold text-gray-900">
                  {data.summary.totalCreatedCurrent}
                </span>
                <span
                  className={`inline-flex items-center text-xs font-bold px-2 py-0.5 rounded-full ${
                    data.summary.createdGrowthRate >= 0
                      ? "bg-emerald-50 text-emerald-700"
                      : "bg-rose-50 text-rose-700"
                  }`}
                >
                  {data.summary.createdGrowthRate >= 0 ? (
                    <TrendingUp className="h-3 w-3 mr-1" />
                  ) : (
                    <TrendingDown className="h-3 w-3 mr-1" />
                  )}
                  {data.summary.createdGrowthRate > 0 ? "+" : ""}
                  {data.summary.createdGrowthRate}%
                </span>
              </div>
              <p className="mt-2 text-[11px] text-gray-400">
                Kỳ trước ({days} ngày trước): {data.summary.totalCreatedPrevious} món
              </p>
            </Card>

            {/* Published Current */}
            <Card className="p-6 rounded-3xl border border-gray-100 bg-white shadow-xs">
              <span className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                Công thức xuất bản ({days} ngày)
              </span>
              <div className="mt-2 flex items-baseline justify-between">
                <span className="text-3xl font-extrabold text-emerald-700">
                  {data.summary.totalPublishedCurrent}
                </span>
                <span
                  className={`inline-flex items-center text-xs font-bold px-2 py-0.5 rounded-full ${
                    data.summary.publishedGrowthRate >= 0
                      ? "bg-emerald-50 text-emerald-700"
                      : "bg-rose-50 text-rose-700"
                  }`}
                >
                  {data.summary.publishedGrowthRate >= 0 ? (
                    <TrendingUp className="h-3 w-3 mr-1" />
                  ) : (
                    <TrendingDown className="h-3 w-3 mr-1" />
                  )}
                  {data.summary.publishedGrowthRate > 0 ? "+" : ""}
                  {data.summary.publishedGrowthRate}%
                </span>
              </div>
              <p className="mt-2 text-[11px] text-gray-400">
                Kỳ trước ({days} ngày trước): {data.summary.totalPublishedPrevious} món
              </p>
            </Card>

            {/* Publication Rate */}
            <Card className="p-6 rounded-3xl border border-gray-100 bg-white shadow-xs">
              <span className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                Tỷ lệ xuất bản thành công
              </span>
              <div className="mt-2">
                <span className="text-3xl font-extrabold text-teal-800">
                  {data.summary.totalCreatedCurrent > 0
                    ? Math.round(
                        (data.summary.totalPublishedCurrent /
                          data.summary.totalCreatedCurrent) *
                          100
                      )
                    : 0}
                  %
                </span>
              </div>
              <p className="mt-2 text-[11px] text-gray-400">
                Tỷ lệ công thức hoàn thiện và xuất bản thực tế
              </p>
            </Card>

            {/* Active Authors Count */}
            <Card className="p-6 rounded-3xl border border-gray-100 bg-white shadow-xs">
              <span className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                Tác giả đóng góp tích cực
              </span>
              <div className="mt-2 flex items-baseline justify-between">
                <span className="text-3xl font-extrabold text-purple-700">
                  {data.topAuthors.length}
                </span>
                <span className="text-xs font-semibold text-purple-600 bg-purple-50 px-2 py-0.5 rounded-full">
                  Top Authors
                </span>
              </div>
              <p className="mt-2 text-[11px] text-gray-400">
                Tác giả có công thức trong {days} ngày qua
              </p>
            </Card>
          </div>

          {/* Activity Trend Chart */}
          <Card className="p-6 sm:p-8 rounded-3xl border border-gray-100 bg-white shadow-xs">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
              <div>
                <h3 className="text-lg font-bold text-gray-900">
                  Xu hướng đăng tải & xuất bản theo ngày
                </h3>
                <p className="text-xs text-gray-500 mt-1">
                  Biểu đồ so sánh số lượng công thức tạo mới vs xuất bản trong {days} ngày gần nhất
                </p>
              </div>

              {/* Legend */}
              <div className="flex items-center gap-4 text-xs font-semibold">
                <div className="flex items-center gap-1.5">
                  <span className="h-3 w-3 rounded-full bg-blue-500" />
                  <span className="text-gray-700">Tạo mới</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <span className="h-3 w-3 rounded-full bg-emerald-600" />
                  <span className="text-gray-700">Đã xuất bản</span>
                </div>
              </div>
            </div>

            {/* Column / Bar Chart Representation */}
            {data.dailyStats.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-12">Chưa có dữ liệu theo ngày</p>
            ) : (
              <div className="space-y-4">
                <div className="h-64 flex items-end gap-2 sm:gap-4 pt-8 pb-4 border-b border-gray-200">
                  {(() => {
                    const maxVal = Math.max(
                      ...data.dailyStats.flatMap((s) => [s.createdCount, s.publishedCount]),
                      4
                    );

                    return data.dailyStats.map((stat, idx) => {
                      const createdHeight = (stat.createdCount / maxVal) * 100;
                      const publishedHeight = (stat.publishedCount / maxVal) * 100;
                      const label = stat.date.slice(5); // MM-DD

                      return (
                        <div
                          key={idx}
                          className="flex-1 flex flex-col items-center justify-end h-full group relative"
                        >
                          {/* Tooltip on hover */}
                          <div className="absolute -top-12 z-20 hidden group-hover:flex flex-col items-center bg-gray-900 text-white text-[10px] rounded-lg px-2 py-1 shadow-lg pointer-events-none whitespace-nowrap">
                            <span className="font-bold">{stat.date}</span>
                            <span>Tạo: {stat.createdCount} | Publish: {stat.publishedCount}</span>
                          </div>

                          <div className="w-full flex items-end justify-center gap-1 sm:gap-1.5 h-full">
                            {/* Created Bar */}
                            <div
                              style={{ height: `${Math.max(createdHeight, 4)}%` }}
                              className="w-1/2 max-w-[18px] bg-blue-500 hover:bg-blue-600 rounded-t-md transition-all duration-300 relative"
                            >
                              {stat.createdCount > 0 && (
                                <span className="absolute -top-4 left-1/2 -translate-x-1/2 text-[9px] font-bold text-blue-700">
                                  {stat.createdCount}
                                </span>
                              )}
                            </div>

                            {/* Published Bar */}
                            <div
                              style={{ height: `${Math.max(publishedHeight, 4)}%` }}
                              className="w-1/2 max-w-[18px] bg-emerald-600 hover:bg-emerald-700 rounded-t-md transition-all duration-300 relative"
                            >
                              {stat.publishedCount > 0 && (
                                <span className="absolute -top-4 left-1/2 -translate-x-1/2 text-[9px] font-bold text-emerald-800">
                                  {stat.publishedCount}
                                </span>
                              )}
                            </div>
                          </div>

                          {/* X-axis date label */}
                          <span className="text-[10px] text-gray-500 font-medium mt-2">
                            {label}
                          </span>
                        </div>
                      );
                    });
                  })()}
                </div>
              </div>
            )}
          </Card>

          {/* Top Authors Table */}
          <Card className="rounded-3xl border border-gray-100 bg-white overflow-hidden shadow-xs">
            <div className="p-6 border-b border-gray-100 flex items-center justify-between">
              <div>
                <h3 className="text-lg font-bold text-gray-900 flex items-center gap-2">
                  <ChefHat className="h-5 w-5 text-emerald-700" />
                  <span>Bảng xếp hạng tác giả hoạt động tích cực</span>
                </h3>
                <p className="text-xs text-gray-500 mt-1">
                  Đánh giá dựa trên số công thức tạo mới và công thức được xuất bản trong {days} ngày qua
                </p>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="bg-gray-50 text-gray-500 font-bold border-b border-gray-100">
                  <tr>
                    <th className="py-3.5 px-6">Thứ hạng</th>
                    <th className="py-3.5 px-6">Tác giả</th>
                    <th className="py-3.5 px-6">Email</th>
                    <th className="py-3.5 px-6 text-center">Công thức tạo mới</th>
                    <th className="py-3.5 px-6 text-center">Công thức xuất bản</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {data.topAuthors.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-8 text-center text-gray-400">
                        Chưa có hoạt động đăng bài trong khoảng thời gian này.
                      </td>
                    </tr>
                  ) : (
                    data.topAuthors.map((author, index) => {
                      const isTop1 = index === 0;
                      const isTop2 = index === 1;
                      const isTop3 = index === 2;

                      return (
                        <tr key={author.authorId} className="hover:bg-gray-50/80 transition-colors">
                          <td className="py-4 px-6 font-bold">
                            <span
                              className={`inline-flex items-center justify-center h-6 w-6 rounded-full text-xs ${
                                isTop1
                                  ? "bg-amber-400 text-amber-950 font-black"
                                  : isTop2
                                  ? "bg-slate-300 text-slate-800 font-bold"
                                  : isTop3
                                  ? "bg-amber-700 text-white font-bold"
                                  : "text-gray-500 bg-gray-100"
                              }`}
                            >
                              {index + 1}
                            </span>
                          </td>
                          <td className="py-4 px-6 font-bold text-gray-900">
                            <div className="flex items-center gap-3">
                              <div className="h-8 w-8 rounded-full bg-emerald-100 text-emerald-800 flex items-center justify-center font-bold text-xs uppercase">
                                {author.authorName ? author.authorName[0] : "U"}
                              </div>
                              <span>{author.authorName}</span>
                            </div>
                          </td>
                          <td className="py-4 px-6 text-gray-500">{author.authorEmail || "—"}</td>
                          <td className="py-4 px-6 text-center font-semibold text-blue-600">
                            {author.recipesCreated}
                          </td>
                          <td className="py-4 px-6 text-center font-bold text-emerald-700">
                            {author.recipesPublished}
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}

export default AdminActivityAnalyticsPage;
