"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { getTrashRecipes } from "@/lib/api/recipes";
import { extractErrorMessage } from "@/lib/api/client";
import type { PagedResult, RecipeTrashItem } from "@/types/recipe";

export default function AdminRecipesTrashPage() {
  const [data, setData] = useState<PagedResult<RecipeTrashItem>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 10,
    totalPages: 1,
  });
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    getTrashRecipes(page, 10)
      .then((res) => {
        setData(res);
        setLoading(false);
      })
      .catch((err) => {
        setError(extractErrorMessage(err, "Không thể tải thùng rác. Yêu cầu quyền Quản trị viên (Admin)."));
        setLoading(false);
      });
  }, [page]);

  return (
    <main className="min-h-screen bg-gray-50/60 py-10 px-4 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-6xl space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 rounded-3xl border border-gray-100 shadow-xs">
          <div>
            <div className="flex items-center gap-2">
              <span className="h-2 w-2 rounded-full bg-purple-600" />
              <span className="text-xs font-bold text-purple-700 uppercase tracking-wider">
                Quản trị viên (Admin Area)
              </span>
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mt-1">
              Thùng rác công thức (Soft Deleted)
            </h1>
            <p className="text-xs text-gray-500">
              Danh sách công thức đã bị xóa mềm, sắp xếp theo thời gian xóa mới nhất (DeletedAt DESC).
            </p>
          </div>

          <Link
            href="/dashboard/statistics"
            className="px-4 py-2 rounded-xl bg-gray-100 hover:bg-gray-200 text-xs font-semibold text-gray-700 transition-colors"
          >
            ← Báo cáo thống kê
          </Link>
        </div>

        {/* Scope Note according to SRS */}
        <div className="rounded-2xl bg-amber-50/80 border border-amber-200 p-4 text-xs text-amber-800 flex items-start gap-3">
          <span className="text-base">ℹ️</span>
          <div>
            <strong className="font-bold">Phân công thành viên 2 (Ánh Nhung):</strong>
            <p className="mt-0.5 text-amber-700">
              Chỉ phụ trách truy vấn danh sách thùng rác (Admin Trash List). Các nghiệp vụ Khôi phục (Restore) và Xóa vĩnh viễn (Purge) thuộc phân công của thành viên phụ trách vòng đời công thức.
            </p>
          </div>
        </div>

        {/* Error message */}
        {error && (
          <div className="rounded-2xl bg-rose-50 border border-rose-200 p-4 text-xs font-semibold text-rose-800 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span>⚠️</span>
              <span>{error}</span>
            </div>
            <button
              onClick={() => setPage(1)}
              className="text-xs underline hover:text-rose-950 font-bold"
            >
              Thử lại
            </button>
          </div>
        )}

        {/* Table of Soft-deleted recipes */}
        <div className="overflow-hidden rounded-3xl bg-white border border-gray-100 shadow-xs">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs text-gray-600">
              <thead className="bg-gray-50/80 text-[11px] font-bold uppercase tracking-wider text-gray-500 border-b border-gray-100">
                <tr>
                  <th className="py-3.5 px-6">Tiêu đề công thức</th>
                  <th className="py-3.5 px-4">Slug định danh</th>
                  <th className="py-3.5 px-4">Trạng thái gốc</th>
                  <th className="py-3.5 px-6">Thời gian xóa (DeletedAt)</th>
                  <th className="py-3.5 px-6 text-center">Ghi chú</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {loading ? (
                  <tr>
                    <td colSpan={5} className="py-12 text-center text-gray-400">
                      Đang tải danh sách công thức đã xóa...
                    </td>
                  </tr>
                ) : data.items.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-12 text-center text-gray-400">
                      Thùng rác trống. Không có công thức nào đang bị xóa mềm.
                    </td>
                  </tr>
                ) : (
                  data.items.map((item) => (
                    <tr key={item.id} className="hover:bg-gray-50/50 transition-colors">
                      <td className="py-4 px-6 font-bold text-gray-900">
                        {item.title}
                      </td>
                      <td className="py-4 px-4 font-mono text-gray-500 text-[11px]">
                        {item.slug}
                      </td>
                      <td className="py-4 px-4">
                        <span className="inline-block rounded-full bg-slate-100 text-slate-700 px-2.5 py-0.5 text-xs font-semibold">
                          {item.status}
                        </span>
                      </td>
                      <td className="py-4 px-6 text-gray-600 font-mono text-[11px]">
                        {item.deletedAt
                          ? new Date(item.deletedAt).toLocaleString("vi-VN")
                          : "—"}
                      </td>
                      <td className="py-4 px-6 text-center text-gray-400 italic text-[11px]">
                        Đã xóa mềm
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {data.totalPages > 1 && (
            <div className="flex items-center justify-between p-4 border-t border-gray-100 text-xs">
              <span className="text-gray-500">
                Trang {data.page} / {data.totalPages} ({data.totalCount} công thức trong thùng rác)
              </span>

              <div className="flex items-center gap-2">
                <button
                  disabled={data.page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  className="px-3 py-1.5 rounded-lg border border-gray-200 text-gray-700 hover:bg-gray-50 disabled:opacity-40"
                >
                  ← Trước
                </button>
                <button
                  disabled={data.page >= data.totalPages}
                  onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
                  className="px-3 py-1.5 rounded-lg border border-gray-200 text-gray-700 hover:bg-gray-50 disabled:opacity-40"
                >
                  Sau →
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </main>
  );
}
