"use client";

import { useEffect } from "react";
import Link from "next/link";

export default function StatisticsError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Dashboard statistics error:", error);
  }, [error]);

  return (
    <main className="min-h-screen bg-gray-50/60 py-16 px-4 sm:px-6 lg:px-8 flex items-center justify-center">
      <div className="max-w-md w-full bg-white rounded-3xl p-8 border border-red-100 shadow-sm text-center space-y-6">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-red-50 text-red-600 text-2xl">
          ⚠️
        </div>

        <div className="space-y-2">
          <h2 className="text-xl font-extrabold text-gray-900">
            Không thể tải dữ liệu thống kê
          </h2>
          <p className="text-xs text-gray-500">
            Hệ thống gặp lỗi khi truy vấn dữ liệu từ API hoặc máy chủ cơ sở dữ liệu tạm thời không phản hồi.
          </p>
          {error.message && (
            <p className="text-[11px] font-mono text-red-600 bg-red-50 p-2 rounded-xl text-left overflow-auto max-h-24">
              {error.message}
            </p>
          )}
        </div>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
          <button
            onClick={() => reset()}
            className="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-[#0d5c3a] text-white text-xs font-semibold hover:bg-[#09432a] transition-colors shadow-xs"
          >
            Thử lại
          </button>
          <Link
            href="/dashboard/categories"
            className="w-full sm:w-auto px-5 py-2.5 rounded-xl bg-gray-100 text-gray-700 text-xs font-semibold hover:bg-gray-200 transition-colors"
          >
            Về Quản lý danh mục
          </Link>
        </div>
      </div>
    </main>
  );
}
