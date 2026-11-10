"use client";

import React from "react";
import ProtectedRoute from "@/components/auth/ProtectedRoute";
import { AdminActivityAnalyticsPage } from "@/components/admin/AdminActivityAnalyticsPage";

export default function AdminActivityAnalyticsRoute() {
  return (
    <ProtectedRoute roles={["Admin"]}>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <AdminActivityAnalyticsPage />
      </div>
    </ProtectedRoute>
  );
}
