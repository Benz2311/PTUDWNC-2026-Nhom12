"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import type { SubmitHandler } from "react-hook-form";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Calendar, Mail, Shield, User as UserIcon, UserCheck, UserX } from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/components/ui/toast";
import { applyFieldErrors } from "@/components/auth/form-errors";
import { ApiError } from "@/lib/api/client";
import type { User } from "@/types/auth";

const profileSchema = z.object({
  displayName: z
    .string()
    .max(100, "Tên hiển thị không được quá 100 ký tự")
    .optional()
    .or(z.literal("")),
  avatarUrl: z
    .string()
    .url("URL ảnh đại diện không hợp lệ")
    .optional()
    .or(z.literal("")),
  bio: z.string().max(500, "Giới thiệu không được quá 500 ký tự").optional(),
});

export type ProfileFormValues = z.infer<typeof profileSchema>;

const inputClasses =
  "w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-100 disabled:bg-gray-50 disabled:text-gray-500 disabled:cursor-not-allowed";
const labelClasses = "mb-1.5 block text-sm font-medium text-gray-700";

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

function getInitials(displayName: string | undefined, userName: string): string {
  const name = displayName ?? userName;
  return name.charAt(0).toUpperCase();
}

interface ProfilePageProps {
  initialUser: User | null;
}

export default function ProfilePage({ initialUser }: ProfilePageProps) {
  const { user, updateProfile, logout } = useAuth();
  const { success, error: showErrorToast } = useToast();
  const [isEditing, setIsEditing] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const currentUser = user ?? initialUser;

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
    watch,
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      displayName: currentUser?.displayName ?? "",
      avatarUrl: currentUser?.avatarUrl ?? "",
      bio: currentUser?.bio ?? "",
    },
  });

  const watchedValues = watch();

  const onSubmit: SubmitHandler<ProfileFormValues> = async (values) => {
    setIsSubmitting(true);
    setFormError(null);

    try {
      await updateProfile({
        displayName: values.displayName ?? "",
        avatarUrl: values.avatarUrl || undefined,
        bio: values.bio || undefined,
      });
      success("Cập nhật hồ sơ thành công");
      setIsEditing(false);
    } catch (err) {
      if (err instanceof ApiError) {
        applyFieldErrors(err, setError);
        setFormError(err.title || "Cập nhật thất bại");
        showErrorToast(err.title || "Cập nhật thất bại");
      } else {
        setFormError("Có lỗi xảy ra, vui lòng thử lại");
        showErrorToast("Có lỗi xảy ra, vui lòng thử lại");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancel = () => {
    reset({
      displayName: currentUser?.displayName ?? "",
      avatarUrl: currentUser?.avatarUrl ?? "",
      bio: currentUser?.bio ?? "",
    });
    setFormError(null);
    setIsEditing(false);
  };

  const handleLogout = async () => {
    try {
      await logout();
      window.location.href = "/login";
    } catch {
      window.location.href = "/login";
    }
  };

  if (!currentUser) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="h-10 w-10 animate-spin rounded-full border-4 border-gray-200 border-t-teal-600" />
      </div>
    );
  }

  const hasChanges =
    watchedValues.displayName !== (currentUser?.displayName ?? "") ||
    watchedValues.avatarUrl !== (currentUser?.avatarUrl ?? "") ||
    watchedValues.bio !== (currentUser?.bio ?? "");

  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <div className="mb-8">
        <h1 className="flex items-center gap-3 text-3xl font-bold text-gray-900">
          <UserIcon className="h-8 w-8 text-teal-600" />
          Hồ sơ cá nhân
        </h1>
        <p className="mt-1 text-gray-500">
          Quản lý thông tin tài khoản và cài đặt cá nhân
        </p>
      </div>

      <div className="space-y-6">
        {/* Avatar & Summary Card */}
        <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm sm:flex sm:items-center sm:gap-6">
          <div className="flex-shrink-0">
            {currentUser.avatarUrl ? (
              <img
                src={currentUser.avatarUrl}
                alt={currentUser.displayName}
                className="h-24 w-24 rounded-full object-cover ring-4 ring-teal-100"
              />
            ) : (
              <div className="flex h-24 w-24 items-center justify-center rounded-full bg-teal-100 text-3xl font-bold text-teal-600 ring-4 ring-teal-100">
                {getInitials(currentUser.displayName, currentUser.userName)}
              </div>
            )}
          </div>

          <div className="mt-4 flex-1 flex items-center justify-between sm:mt-0 sm:flex-col sm:items-start sm:justify-start">
            <div>
              <h2 className="text-2xl font-bold text-gray-900">
                {currentUser.displayName || currentUser.userName}
              </h2>
              <p className="mt-1 text-sm text-gray-500">@{currentUser.userName}</p>

              <div className="mt-3 flex flex-wrap items-center gap-2">
                {!currentUser.emailConfirmed ? (
                  <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-medium text-amber-800">
                    <UserX className="h-3 w-3" />
                    Chưa xác thực email
                  </span>
                ) : (
                  <span className="inline-flex items-center gap-1.5 rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-800">
                    <UserCheck className="h-3 w-3" />
                    Email đã xác thực
                  </span>
                )}

                {currentUser.roles && currentUser.roles.length > 0 && (
                  <span className="inline-flex items-center gap-1.5 rounded-full border border-purple-200 bg-purple-50 px-2.5 py-0.5 text-xs font-medium text-purple-700">
                    <Shield className="h-3 w-3" />
                    {currentUser.roles.map((r) => (r === "Admin" ? "Quản trị viên" : r === "Author" ? "Tác giả" : "Người dùng")).join(", ")}
                  </span>
                )}
              </div>

              {currentUser.createdAt && (
                <p className="mt-3 flex items-center gap-1.5 text-sm text-gray-500">
                  <Calendar className="h-4 w-4" />
                  Tham gia ngày {formatDate(currentUser.createdAt)}
                </p>
              )}
            </div>

            {!isEditing && (
              <button
                type="button"
                onClick={() => setIsEditing(true)}
                className="mt-4 ml-4 flex-shrink-0 rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700 sm:mt-0 sm:ml-0"
              >
                Chỉnh sửa hồ sơ
              </button>
            )}
          </div>
        </div>

        {/* Read-only Info Card */}
        <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
          <h3 className="flex items-center gap-2 text-lg font-semibold text-gray-900">
            <Mail className="h-5 w-5 text-teal-600" />
            Thông tin tài khoản (Chỉ đọc)
          </h3>
          <p className="mt-1 text-sm text-gray-500">
            Email và tên đăng nhập không thể thay đổi. Liên hệ quản trị viên nếu cần hỗ trợ.
          </p>

          <dl className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <dt className="text-sm font-medium text-gray-500">Tên đăng nhập</dt>
              <dd className="mt-1.5 flex items-center gap-2 rounded-lg bg-gray-50 px-3 py-2.5 text-sm font-mono text-gray-900">
                <UserIcon className="h-4 w-4 text-gray-400" />
                <span>{currentUser.userName}</span>
              </dd>
            </div>

            <div>
              <dt className="text-sm font-medium text-gray-500">Email</dt>
              <dd className="mt-1.5 flex items-center gap-2 rounded-lg bg-gray-50 px-3 py-2.5 text-sm font-mono text-gray-900">
                <Mail className="h-4 w-4 text-gray-400" />
                <span>{currentUser.email}</span>
              </dd>
            </div>

            {currentUser.roles && currentUser.roles.length > 0 && (
              <div className="sm:col-span-2">
                <dt className="text-sm font-medium text-gray-500">Vai trò</dt>
                <dd className="mt-1.5 flex flex-wrap gap-1.5">
                  {currentUser.roles.map((role) => (
                    <span
                      key={role}
                      className={`inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-medium ${
                        role === "Admin"
                          ? "border-purple-200 bg-purple-50 text-purple-700"
                          : role === "Author"
                          ? "border-blue-200 bg-blue-50 text-blue-700"
                          : "border-gray-200 bg-gray-50 text-gray-700"
                      }`}
                    >
                      <Shield className="h-3 w-3" />
                      {role === "Admin"
                        ? "Quản trị viên"
                        : role === "Author"
                        ? "Tác giả"
                        : "Người dùng"}
                    </span>
                  ))}
                </dd>
              </div>
            )}

            {currentUser.createdAt && (
              <div className="sm:col-span-2">
                <dt className="text-sm font-medium text-gray-500">Ngày tham gia</dt>
                <dd className="mt-1.5 flex items-center gap-2 rounded-lg bg-gray-50 px-3 py-2.5 text-sm text-gray-900">
                  <Calendar className="h-4 w-4 text-gray-400" />
                  <span>{formatDate(currentUser.createdAt)}</span>
                </dd>
              </div>
            )}
          </dl>
        </div>

        {/* Update Profile Form */}
        <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
          <h3 className="flex items-center gap-2 text-lg font-semibold text-gray-900">
            <UserIcon className="h-5 w-5 text-teal-600" />
            Cập nhật hồ sơ
          </h3>

          {!isEditing ? (
            <div className="mt-4 text-center py-8">
              <p className="text-gray-500">
                Nhấn nút "Chỉnh sửa hồ sơ" ở trên để cập nhật thông tin cá nhân.
              </p>
              <button
                type="button"
                onClick={() => setIsEditing(true)}
                className="mt-4 rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700"
              >
                Chỉnh sửa hồ sơ
              </button>
            </div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} className="mt-4 space-y-5" noValidate>
              {formError && (
                <div
                  role="alert"
                  className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
                >
                  {formError}
                </div>
              )}

              <div>
                <label htmlFor="displayName" className={labelClasses}>
                  Tên hiển thị <span className="text-red-500">*</span>
                </label>
                <input
                  id="displayName"
                  type="text"
                  className={inputClasses}
                  {...register("displayName")}
                  disabled={isSubmitting}
                />
                {errors.displayName && (
                  <p className="mt-1.5 text-sm text-red-600">
                    {errors.displayName.message}
                  </p>
                )}
              </div>

              <div>
                <label htmlFor="avatarUrl" className={labelClasses}>
                  URL ảnh đại diện
                </label>
                <input
                  id="avatarUrl"
                  type="url"
                  className={inputClasses}
                  placeholder="https://example.com/avatar.jpg"
                  {...register("avatarUrl")}
                  disabled={isSubmitting}
                />
                {errors.avatarUrl && (
                  <p className="mt-1.5 text-sm text-red-600">
                    {errors.avatarUrl.message}
                  </p>
                )}
                {currentUser.avatarUrl && (
                  <p className="mt-1.5 text-xs text-gray-500">
                    Ảnh hiện tại:{" "}
                    <a
                      href={currentUser.avatarUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-teal-600 hover:underline"
                    >
                      Xem
                    </a>
                  </p>
                )}
              </div>

              <div>
                <label htmlFor="bio" className={labelClasses}>
                  Giới thiệu
                </label>
                <textarea
                  id="bio"
                  rows={4}
                  className={inputClasses}
                  placeholder="Giới thiệu ngắn về bạn..."
                  {...register("bio")}
                  disabled={isSubmitting}
                />
                {errors.bio && (
                  <p className="mt-1.5 text-sm text-red-600">{errors.bio.message}</p>
                )}
                <p className="mt-1.5 text-xs text-gray-500">
                  Tối đa 500 ký tự ({watchedValues.bio?.length ?? 0}/500)
                </p>
              </div>

              {/* Read-only fields display */}
              <div className="pt-4 border-t border-gray-100">
                <p className="mb-3 text-sm font-medium text-gray-700">
                  Các trường dưới đây không thể chỉnh sửa:
                </p>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label htmlFor="email" className={labelClasses}>
                      Email
                    </label>
                    <input
                      id="email"
                      type="email"
                      value={currentUser.email}
                      className={inputClasses}
                      readOnly
                      aria-readonly="true"
                    />
                  </div>
                  <div>
                    <label htmlFor="username" className={labelClasses}>
                      Tên đăng nhập
                    </label>
                    <input
                      id="username"
                      type="text"
                      value={currentUser.userName}
                      className={inputClasses}
                      readOnly
                      aria-readonly="true"
                    />
                  </div>
                </div>
              </div>

              <div className="pt-4 border-t border-gray-100 flex gap-3">
                <button
                  type="submit"
                  disabled={isSubmitting || !hasChanges}
                  className="rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700 disabled:opacity-60 disabled:cursor-not-allowed"
                >
                  {isSubmitting ? "Đang lưu..." : "Lưu thay đổi"}
                </button>
                <button
                  type="button"
                  onClick={handleCancel}
                  disabled={isSubmitting}
                  className="rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50 disabled:opacity-60"
                >
                  Hủy
                </button>
              </div>
            </form>
          )}
        </div>

        {/* Logout Section */}
        {!isEditing && (
          <div className="rounded-2xl border border-red-200 bg-red-50 p-6 shadow-sm">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="font-semibold text-red-800">Đăng xuất</h3>
                <p className="mt-1 text-sm text-red-600">
                  Bạn sẽ bị đăng xuất khỏi tất cả thiết bị.
                </p>
              </div>
              <button
                type="button"
                onClick={handleLogout}
                className="rounded-lg bg-red-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-red-700"
              >
                Đăng xuất
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}