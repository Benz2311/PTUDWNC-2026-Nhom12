import type { Metadata } from "next";
import ProtectedRoute from "@/components/auth/ProtectedRoute";
import AdminCategoriesPage from "@/components/admin/AdminCategoriesPage";

export const metadata: Metadata = {
  title: "Quản lý danh mục | Admin",
  description: "Trang quản trị danh mục công thức ẩm thực",
};

export default function AdminCategoriesRoute() {
  return (
    <ProtectedRoute roles={["Admin"]}>
      <AdminCategoriesPage />
    </ProtectedRoute>
  );
}

