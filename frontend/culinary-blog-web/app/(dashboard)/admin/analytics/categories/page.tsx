"use client";

import React from "react";
import ProtectedRoute from "@/components/auth/ProtectedRoute";
import { AdminCategoryPerformancePage } from "@/components/admin/AdminCategoryPerformancePage";

export default function AdminCategoryPerformanceRoute() {
  return (
    <ProtectedRoute roles={["Admin"]}>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <AdminCategoryPerformancePage />
      </div>
    </ProtectedRoute>
  );
}
