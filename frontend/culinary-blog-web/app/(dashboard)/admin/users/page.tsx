import type { Metadata } from "next";
import ProtectedRoute from "@/components/auth/ProtectedRoute";
import AdminUsersPage from "@/components/admin/AdminUsersPage";

export const metadata: Metadata = {
  title: "Quản lý người dùng",
};

export default function AdminUsersRoute() {
  return (
    <ProtectedRoute roles={["Admin"]}>
      <AdminUsersPage />
    </ProtectedRoute>
  );
}
