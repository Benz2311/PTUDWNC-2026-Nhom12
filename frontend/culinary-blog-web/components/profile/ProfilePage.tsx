"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import Image from "next/image";
import type { SubmitHandler } from "react-hook-form";
import { useForm } from "react-hook-form";
import { z } from "zod";
import {
  Calendar,
  Mail,
  PenTool,
  RefreshCw,
  Shield,
  User as UserIcon,
  UserCheck,
  UserX,
} from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/components/ui/toast";
import { applyFieldErrors } from "@/components/auth/form-errors";
import { authApi } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";
import type { User } from "@/types/auth";
import ChangePasswordCard from "@/components/profile/ChangePasswordCard";

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

function getRoleLabel(role: string): string {
  if (role === "Admin") return "Quản trị viên";
  if (role === "Author") return "Tác giả";
  return "Người dùng";
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

  // Trạng thái gửi lại email xác thực
  const [isResending, setIsResending] = useState(false);
  const [resendMessage, setResendMessage] = useState<{
    type: "success" | "error";
    text: string;
  } | null>(null);

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
      const updated = await updateProfile({
        displayName: values.displayName ?? "",
        avatarUrl: values.avatarUrl || undefined,
        bio: values.bio || undefined,
      });
      reset({
        displayName: updated.displayName ?? "",
        avatarUrl: updated.avatarUrl ?? "",
        bio: updated.bio ?? "",
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

  // Gửi lại email xác thực (chỉ khi email chưa xác thực)
  const handleResendConfirmation = async () => {
    if (!currentUser?.email) {
      return;
    }

    setIsResending(true);
    setResendMessage(null);

    try {
      await authApi.resendVerificationEmail(currentUser.email);
      setResendMessage({
        type: "success",
        text: "Đã gửi lại email xác thực. Vui lòng kiểm tra hộp thư (bao gồm thư mục spam).",
      });
    } catch (err) {
      if (err instanceof ApiError && err.status === 429) {
        setResendMessage({
          type: "error",
          text: "Quá nhiều yêu cầu. Vui lòng thử lại sau.",
        });
      } else {
        setResendMessage({
          type: "error",
          text: "Không thể gửi lại email. Vui lòng thử lại sau.",
        });
      }
    } finally {
      setIsResending(false);
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

  const roleBadges = currentUser.roles ?? [];

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      {/* Header trang — gọn, không chiếm nhiều chiều cao */}
      <header className="mb-6">
        <h1 className="flex items-center gap-3 text-2xl font-bold text-gray-900">
          <UserIcon className="h-7 w-7 text-teal-600" />
          Hồ sơ cá nhân
        </h1>
        <p className="mt-1 text-sm text-gray-500">
          Quản lý thông tin tài khoản và cài đặt cá nhân
        </p>
      </header>

      {/* Bố cục hai cột desktop: trái = thông tin hồ sơ, phải = tác vụ */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-10">
        {/* ── Cột trái: Thông tin hồ sơ & tài khoản ────────────────── */}
        <section className="lg:col-span-4">
          <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
            <h2 className="text-lg font-semibold text-gray-900">
              Thông tin hồ sơ
            </h2>

            <div className="mt-5 flex flex-col items-center text-center">
              {/* Avatar — fallback chữ cái đầu nếu ảnh không tải được */}
              <div className="relative flex h-20 w-20 items-center justify-center rounded-full bg-teal-100 text-xl font-bold text-teal-600 ring-4 ring-teal-100">
                <span>{getInitials(currentUser.displayName, currentUser.userName)}</span>
                {currentUser.avatarUrl && (
                  <Image
                    src={currentUser.avatarUrl}
                    alt={currentUser.displayName}
                    width={80}
                    height={80}
                    unoptimized
                    className="absolute inset-0 h-full w-full rounded-full object-cover"
                    onError={(event) => {
                      event.currentTarget.classList.add("hidden");
                    }}
                  />
                )}
              </div>

              <h3 className="mt-4 text-xl font-bold text-gray-900">
                {currentUser.displayName || currentUser.userName}
              </h3>
              <p className="mt-0.5 text-sm text-gray-500">
                @{currentUser.userName}
              </p>

              <div className="mt-3 flex flex-wrap items-center justify-center gap-2">
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

                {roleBadges.map((role) => (
                  <span
                    key={role}
                    className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium ${
                      role === "Admin"
                        ? "border-purple-200 bg-purple-50 text-purple-700"
                        : role === "Author"
                          ? "border-blue-200 bg-blue-50 text-blue-700"
                          : "border-gray-200 bg-gray-50 text-gray-700"
                    }`}
                  >
                    <Shield className="h-3 w-3" />
                    {getRoleLabel(role)}
                  </span>
                ))}
              </div>
            </div>

            {/* Thông tin tài khoản chỉ đọc — hiển thị dạng text, không phải input */}
            <dl className="mt-6 space-y-3 border-t border-gray-100 pt-5 text-sm">
              <div className="flex items-start gap-2.5">
                <Mail className="mt-0.5 h-4 w-4 flex-shrink-0 text-gray-400" />
                <span className="break-all font-mono text-gray-900">
                  {currentUser.email}
                </span>
              </div>
              <div className="flex items-center gap-2.5">
                <Calendar className="h-4 w-4 flex-shrink-0 text-gray-400" />
                <span className="text-gray-500">
                  Tham gia ngày {formatDate(currentUser.createdAt)}
                </span>
              </div>
            </dl>
          </div>
        </section>

        {/* ── Cột phải: Chỉnh sửa hồ sơ & xác thực email ────────────── */}
        <section className="space-y-6 lg:col-span-6">
          {/* Card chỉnh sửa — một điểm mở chế độ chỉnh sửa, một form duy nhất */}
          <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
            <h2 className="flex items-center gap-2 text-lg font-semibold text-gray-900">
              <UserIcon className="h-5 w-5 text-teal-600" />
              Chỉnh sửa thông tin cá nhân
            </h2>

            {!isEditing ? (
              /* Trạng thái xem */
              <div className="mt-4">
                <dl className="space-y-4 text-sm">
                  <div>
                    <dt className="text-sm font-medium text-gray-500">
                      Tên hiển thị
                    </dt>
                    <dd className="mt-1 text-gray-900">
                      {currentUser.displayName || "—"}
                    </dd>
                  </div>

                  <div>
                    <dt className="text-sm font-medium text-gray-500">
                      Ảnh đại diện
                    </dt>
                    <dd className="mt-1 flex items-center gap-3">
                      {currentUser.avatarUrl ? (
                        <>
                          <span className="relative flex h-12 w-12 flex-shrink-0 items-center justify-center overflow-hidden rounded-full bg-teal-100 text-sm font-bold text-teal-600">
                            <span>
                              {getInitials(currentUser.displayName, currentUser.userName)}
                            </span>
                            <Image
                    src={currentUser.avatarUrl}
                    alt={currentUser.displayName}
                    width={80}
                    height={80}
                    unoptimized
                    className="absolute inset-0 h-full w-full rounded-full object-cover"
                    onError={(event) => {
                      event.currentTarget.classList.add("hidden");
                    }}
                  />
                          </span>
                          <a
                            href={currentUser.avatarUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="break-all text-teal-600 hover:underline"
                          >
                            {currentUser.avatarUrl}
                          </a>
                        </>
                      ) : (
                        <span className="text-gray-500">Chưa đặt ảnh đại diện</span>
                      )}
                    </dd>
                  </div>

                  <div>
                    <dt className="text-sm font-medium text-gray-500">
                      Giới thiệu
                    </dt>
                    <dd className="mt-1 whitespace-pre-wrap text-gray-900">
                      {currentUser.bio || "Chưa có giới thiệu"}
                    </dd>
                  </div>
                </dl>

                <button
                  type="button"
                  onClick={() => setIsEditing(true)}
                  className="mt-5 rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700"
                >
                  Chỉnh sửa hồ sơ
                </button>
              </div>
            ) : (
              /* Trạng thái chỉnh sửa */
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

                  {/* Avatar preview — chỉ lưu sau khi người dùng xác nhận lưu */}
                  {watchedValues.avatarUrl ? (
                    <div className="mt-3 flex items-center gap-3">
                      <span className="relative flex h-16 w-16 flex-shrink-0 items-center justify-center overflow-hidden rounded-full bg-teal-100 text-sm font-bold text-teal-600 ring-2 ring-teal-100">
                        <span>
                          {getInitials(currentUser.displayName, currentUser.userName)}
                        </span>
                        <Image
                          src={watchedValues.avatarUrl}
                          alt="Xem trước ảnh đại diện"
                          width={64}
                          height={64}
                          unoptimized
                          className="absolute inset-0 h-full w-full rounded-full object-cover"
                          onError={(event) => {
                            event.currentTarget.classList.add("hidden");
                          }}
                        />
                      </span>
                      <p className="text-xs text-gray-500">
                        Xem trước ảnh đại diện (chưa lưu)
                      </p>
                    </div>
                  ) : null}
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

                <div className="flex gap-3 border-t border-gray-100 pt-4">
                  <button
                    type="submit"
                    disabled={isSubmitting || !hasChanges}
                    className="rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {isSubmitting ? "Đang lưu..." : "Lưu thay đổi"}
                  </button>
                  <button
                    type="button"
                    onClick={handleCancel}
                    disabled={isSubmitting}
                    className="rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Hủy
                  </button>
                </div>
              </form>
            )}
          </div>

          {/* Card xác thực email — gọn, gần khu vực chỉnh sửa */}
          {!currentUser.emailConfirmed ? (
            <div className="rounded-2xl border border-amber-200 bg-amber-50 p-5 shadow-sm">
              <div className="flex items-start gap-3">
                <UserX className="mt-0.5 h-5 w-5 flex-shrink-0 text-amber-600" />
                <div className="flex-1">
                  <h3 className="font-semibold text-amber-800">
                    Email của bạn chưa được xác thực
                  </h3>
                  <p className="mt-1 text-sm text-amber-700">
                    Vui lòng kiểm tra hộp thư{" "}
                    <span className="font-medium">{currentUser.email}</span> và nhấp vào liên kết xác thực. Nếu chưa nhận được, bạn có thể gửi lại thư xác thực.
                  </p>

                  <div className="mt-4">
                    <button
                      type="button"
                      onClick={handleResendConfirmation}
                      disabled={isResending}
                      className="inline-flex items-center gap-2 rounded-lg bg-amber-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-amber-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      <RefreshCw className={`h-4 w-4 ${isResending ? "animate-spin" : ""}`} />
                      {isResending ? "Đang gửi..." : "Gửi lại email xác thực"}
                    </button>
                  </div>

                  {resendMessage && (
                    <p
                      role="status"
                      className={`mt-3 text-sm ${
                        resendMessage.type === "success"
                          ? "text-green-700"
                          : "text-red-700"
                      }`}
                    >
                      {resendMessage.text}
                    </p>
                  )}
                </div>
              </div>
            </div>
          ) : (
            <div className="rounded-2xl border border-green-200 bg-green-50 p-5 shadow-sm">
              <div className="flex items-center gap-3">
                <span className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-green-100">
                  <UserCheck className="h-4 w-4 text-green-700" />
                </span>
                <div>
                  <p className="text-sm font-semibold text-green-800">
                    Email đã được xác thực
                  </p>
                  <p className="text-xs text-green-700">
                    Tài khoản của bạn đã được xác nhận qua email.
                  </p>
                </div>
              </div>
            </div>
          )}
        </section>
      </div>

      {/* Lối tắt chức năng — một hàng liên kết nhỏ, theo role, ở cuối bố cục chính */}
      {(currentUser.roles?.includes("Author") ||
        currentUser.roles?.includes("Admin")) && (
        <nav className="mt-6 flex flex-wrap items-center gap-3" aria-label="Lối tắt chức năng">
          <span className="text-sm font-medium text-gray-700">Lối tắt:</span>

          {currentUser.roles?.includes("Author") && (
            <a
              href="/dashboard/recipes"
              className="inline-flex items-center gap-2 rounded-full border border-gray-200 bg-white px-3.5 py-1.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
            >
              <PenTool className="h-4 w-4 text-blue-600" />
              Khu vực tác giả
            </a>
          )}

          {currentUser.roles?.includes("Admin") && (
            <a
              href="/admin/users"
              className="inline-flex items-center gap-2 rounded-full border border-gray-200 bg-white px-3.5 py-1.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
            >
              <Shield className="h-4 w-4 text-purple-600" />
              Khu vực quản trị
            </a>
          )}
        </nav>
      )}

      {/* Đăng xuất — chức năng riêng biệt, không trùng lặp */}
      <div className="mt-6 rounded-2xl border border-red-200 bg-red-50 p-5 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3">
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
      <ChangePasswordCard />
    </div>
  );
}
