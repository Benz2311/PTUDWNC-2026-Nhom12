"use client";

import React, { useState } from "react";
import ProtectedRoute from "@/components/auth/ProtectedRoute";
import { AdminActivityAnalyticsPage } from "@/components/admin/AdminActivityAnalyticsPage";
import { AdminCategoryPerformancePage } from "@/components/admin/AdminCategoryPerformancePage";
import { BarChart3, PieChart } from "lucide-react";

export default function AdminAnalyticsRoute() {
  const [activeTab, setActiveTab] = useState<"activity" | "categories">("activity");

  return (
    <ProtectedRoute roles={["Admin"]}>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
        {/* Navigation Tabs */}
        <div className="flex border-b border-gray-200">
          <button
            type="button"
            onClick={() => setActiveTab("activity")}
            className={`flex items-center gap-2 py-3 px-5 text-sm font-bold border-b-2 transition-all cursor-pointer ${
              activeTab === "activity"
                ? "border-emerald-700 text-emerald-800"
                : "border-transparent text-gray-500 hover:text-gray-900 hover:border-gray-300"
            }`}
          >
            <BarChart3 className="h-4 w-4" />
            <span>Hoạt động đăng tải (Activity Analytics)</span>
          </button>

          <button
            type="button"
            onClick={() => setActiveTab("categories")}
            className={`flex items-center gap-2 py-3 px-5 text-sm font-bold border-b-2 transition-all cursor-pointer ${
              activeTab === "categories"
                ? "border-emerald-700 text-emerald-800"
                : "border-transparent text-gray-500 hover:text-gray-900 hover:border-gray-300"
            }`}
          >
            <PieChart className="h-4 w-4" />
            <span>Hiệu quả danh mục (Category Performance)</span>
          </button>
        </div>

        {/* Tab Content */}
        {activeTab === "activity" ? (
          <AdminActivityAnalyticsPage />
        ) : (
          <AdminCategoryPerformancePage />
        )}
      </div>
    </ProtectedRoute>
  );
}
