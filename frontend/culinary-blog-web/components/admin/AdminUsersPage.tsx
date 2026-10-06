"use client";

import { useCallback, useEffect, useState } from "react";
import { Calendar, RotateCcw, Search, SearchX, Users } from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/components/ui/toast";
import { adminApi } from "@/lib/api/admin";
import { ApiError } from "@/lib/api/client";
import type { AdminUserDto, PagedUsersResponse } from "@/types/admin";
import ConfirmationModal from "./ConfirmationModal";
import Pagination from "./Pagination";
import RoleBadge from "./RoleBadge";
import UserStatusBadge from "./UserStatusBadge";
import UserTableSkeleton from "./UserTableSkeleton";

const PAGE_SIZE = 10;
const DEBOUNCE_MS = 400;

type StatusFilter = "all" | "active" | "inactive";

const STATUS_OPTIONS: { value: StatusFilter; label: string }[] = [
  { value: "all", label: "Tất cả" },
  { value: "active", label: "Hoạt động" },
  { value: "inactive", label: "Vô hiệu hóa" },
];

const SELF_DISABLE_MESSAGE =
  "Bạn không thể tự vô hiệu hóa tài khoản của chính mình!";

function formatDate(isoDate: string): string {
  const date = new Date(isoDate);
  if (Number.isNaN(date.getTime())) {
    return "—";
  }
  return date.toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

export default function AdminUsersPage() {
  const { user: currentUser } = useAuth();
  const { success, error: showErrorToast } = useToast();

  const [data, setData] = useState<PagedUsersResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [keyword, setKeyword] = useState("");
  const [debouncedKeyword, setDebouncedKeyword] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [page, setPage] = useState(1);

  const [pendingUser, setPendingUser] = useState<AdminUserDto | null>(null);
  const [isUpdating, setIsUpdating] = useState(false);

  // Debounce từ khóa tìm kiếm
  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedKeyword(keyword);
    }, DEBOUNCE_MS);
    return () => window.clearTimeout(timer);
  }, [keyword]);

  // Reset về trang 1 khi bộ lọc thay đổi
  useEffect(() => {
    setPage(1);
  }, [debouncedKeyword, statusFilter]);

  const loadUsers = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await adminApi.getUsers({
        page,
        pageSize: PAGE_SIZE,
        keyword: debouncedKeyword.trim() || undefined,
        isActive:
          statusFilter === "all" ? undefined : statusFilter === "active",
      });

      // Trang hiện tại rỗng do phần tử vừa bị lọc → lùi về trang trước
      if (result.items.length === 0 && result.page > 1) {
        setPage(result.page - 1);
        return;
      }

      setData(result);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.title || "Không thể tải danh sách người dùng");
      } else {
        setError("Không thể kết nối đến máy chủ. Vui lòng thử lại sau.");
      }
    } finally {
      setIsLoading(false);
    }
  }, [page, debouncedKeyword, statusFilter]);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  const handleToggleClick = (user: AdminUserDto) => {
    setPendingUser(user);
  };

  const handleConfirmStatusChange = async () => {
    if (!pendingUser) {
      return;
    }

    const nextStatus = !pendingUser.isActive;

    // Bảo vệ phía client: Admin không tự vô hiệu hóa chính mình
    if (!nextStatus && currentUser?.id === pendingUser.id) {
      showErrorToast(SELF_DISABLE_MESSAGE);
      setPendingUser(null);
      return;
    }

    setIsUpdating(true);

    try {
      await adminApi.updateUserStatus(pendingUser.id, nextStatus);
      success(
        nextStatus
          ? `Đã kích hoạt tài khoản ${pendingUser.displayName}`
          : `Đã vô hiệu hóa tài khoản ${pendingUser.displayName}`
      );
      setPendingUser(null);
      await loadUsers();
    } catch (err) {
      // Backend trả 400 RFC 7807 khi Admin cố tự khóa chính mình
      if (err instanceof ApiError && err.status === 400) {
        showErrorToast(SELF_DISABLE_MESSAGE);
      } else if (err instanceof ApiError) {
        showErrorToast(err.title || "Cập nhật trạng thái thất bại");
      } else {
        showErrorToast("Có lỗi xảy ra, vui lòng thử lại");
      }
    } finally {
      setIsUpdating(false);
    }
  };

  const handleResetFilters = () => {
    setKeyword("");
    setStatusFilter("all");
    setPage(1);
  };

  const users = data?.items ?? [];
  const totalPages = data?.totalPages ?? 0;
  const totalCount = data?.totalCount ?? 0;

  return (
    <main className="min-h-screen bg-gray-50 p-8">
      <div className="mx-auto max-w-7xl">
        <div className="mb-8">
          <h1 className="flex items-center gap-3 text-3xl font-bold text-gray-900">
            <Users className="h-8 w-8 text-teal-600" />
            Quản lý người dùng
          </h1>
          <p className="mt-1 text-gray-500">
            {data
              ? `Tổng cộng ${totalCount} người dùng`
              : "Quản lý tài khoản và quyền truy cập"}
          </p>
        </div>

        {/* Search & Filter Bar */}
        <div className="mb-6 flex flex-col gap-4 rounded-2xl border border-gray-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              value={keyword}
              onChange={(event) => setKeyword(event.target.value)}
              placeholder="Tìm kiếm theo tên, email hoặc tên đăng nhập..."
              aria-label="Tìm kiếm người dùng"
              className="w-full rounded-lg border border-gray-300 bg-white py-2.5 pl-10 pr-4 text-sm text-gray-900 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-100"
            />
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <label
              htmlFor="statusFilter"
              className="text-sm font-medium text-gray-700"
            >
              Trạng thái:
            </label>
            <select
              id="statusFilter"
              value={statusFilter}
              onChange={(event) =>
                setStatusFilter(event.target.value as StatusFilter)
              }
              className="rounded-lg border border-gray-300 bg-white px-3 py-2.5 text-sm text-gray-900 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-100"
            >
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>

            {(keyword || statusFilter !== "all") && (
              <button
                type="button"
                onClick={handleResetFilters}
                className="flex items-center gap-1.5 rounded-lg border border-gray-300 px-3 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
              >
                <RotateCcw className="h-3.5 w-3.5" />
                Xóa bộ lọc
              </button>
            )}
          </div>
        </div>

        {/* Content */}
        {error ? (
          <div className="rounded-2xl border border-red-200 bg-red-50 p-12 text-center">
            <p className="text-sm font-medium text-red-700">{error}</p>
            <button
              type="button"
              onClick={loadUsers}
              className="mt-4 rounded-lg bg-red-500 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-red-600"
            >
              Thử lại
            </button>
          </div>
        ) : isLoading ? (
          <UserTableSkeleton />
        ) : users.length === 0 ? (
          <div className="rounded-2xl border border-gray-200 bg-white p-12 text-center">
            <SearchX className="mx-auto h-12 w-12 text-gray-300" />
            <h3 className="mt-4 text-lg font-semibold text-gray-900">
              Không tìm thấy người dùng nào
            </h3>
            <p className="mt-1 text-sm text-gray-500">
              Thử thay đổi từ khóa hoặc bộ lọc trạng thái.
            </p>
            <button
              type="button"
              onClick={handleResetFilters}
              className="mt-6 rounded-lg bg-teal-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-teal-700"
            >
              Xóa bộ lọc
            </button>
          </div>
        ) : (
          <div className="overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-sm">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[960px] text-left text-sm">
                <thead className="border-b border-gray-200 bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                      Người dùng
                    </th>
                    <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                      Tên đăng nhập
                    </th>
                    <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                      Email
                    </th>
                    <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                      Vai trò
                    </th>
                    <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                      Trạng thái
                    </th>
                    <th className="px-6 py-3 text-xs font-semibold uppercase tracking-wider text-gray-500">
                      Ngày tạo
                    </th>
                    <th className="px-6 py-3 text-right text-xs font-semibold uppercase tracking-wider text-gray-500">
                      Hành động
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {users.map((user) => (
                    <tr
                      key={user.id}
                      className="transition-colors hover:bg-gray-50"
                    >
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-3">
                          {user.avatarUrl ? (
                            <img
                              src={user.avatarUrl}
                              alt={user.displayName}
                              className="h-10 w-10 rounded-full object-cover"
                            />
                          ) : (
                            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-teal-100 text-sm font-semibold text-teal-700">
                              {(user.displayName || user.userName)
                                .charAt(0)
                                .toUpperCase()}
                            </div>
                          )}
                          <span className="font-medium text-gray-900">
                            {user.displayName}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4 text-gray-600">
                        @{user.userName}
                      </td>
                      <td className="px-6 py-4 text-gray-600">{user.email}</td>
                      <td className="px-6 py-4">
                        <div className="flex flex-wrap gap-1.5">
                          {user.roles.map((role) => (
                            <RoleBadge key={role} role={role} />
                          ))}
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <UserStatusBadge isActive={user.isActive} />
                      </td>
                      <td className="px-6 py-4">
                        <span className="flex items-center gap-1.5 whitespace-nowrap text-gray-600">
                          <Calendar className="h-3.5 w-3.5 text-gray-400" />
                          {formatDate(user.createdAt)}
                        </span>
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center justify-end">
                          <button
                            type="button"
                            role="switch"
                            aria-checked={user.isActive}
                            aria-label={
                              user.isActive
                                ? `Vô hiệu hóa tài khoản ${user.displayName}`
                                : `Kích hoạt tài khoản ${user.displayName}`
                            }
                            onClick={() => handleToggleClick(user)}
                            className={`relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-teal-200 ${
                              user.isActive
                                ? "bg-green-500"
                                : "bg-gray-300"
                            }`}
                          >
                            <span
                              className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
                                user.isActive
                                  ? "translate-x-6"
                                  : "translate-x-1"
                              }`}
                            />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="border-t border-gray-200 px-6 py-4">
              <Pagination
                page={page}
                totalPages={totalPages}
                totalCount={totalCount}
                onPageChange={setPage}
              />
            </div>
          </div>
        )}

        {/* Confirmation Modal */}
        <ConfirmationModal
          isOpen={pendingUser !== null}
          title={
            pendingUser?.isActive ? "Vô hiệu hóa tài khoản" : "Kích hoạt tài khoản"
          }
          message={
            <>
              Bạn có chắc chắn muốn{" "}
              <strong>
                {pendingUser?.isActive ? "vô hiệu hóa" : "kích hoạt"}
              </strong>{" "}
              tài khoản{" "}
              <strong className="text-gray-900">
                {pendingUser?.displayName}
              </strong>
              ?
              {pendingUser?.isActive && (
                <span className="mt-2 block text-sm text-gray-500">
                  Người dùng sẽ không thể đăng nhập cho đến khi tài khoản được
                  kích hoạt lại.
                </span>
              )}
            </>
          }
          confirmLabel={
            pendingUser?.isActive ? "Vô hiệu hóa" : "Kích hoạt"
          }
          variant={pendingUser?.isActive ? "danger" : "success"}
          isLoading={isUpdating}
          onConfirm={handleConfirmStatusChange}
          onCancel={() => setPendingUser(null)}
        />
      </div>
    </main>
  );
}
