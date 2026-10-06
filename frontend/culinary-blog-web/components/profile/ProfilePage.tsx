"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import type { SubmitHandler } from "react-hook-form";
import { useForm } from "react-hook-form";
import { z } from "zod";
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
  "w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-100";
const labelClasses = "mb-1.5 block text-sm font-medium text-gray-700";

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
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      displayName: currentUser?.displayName ?? "",
      avatarUrl: currentUser?.avatarUrl ?? "",
      bio: currentUser?.bio ?? "",
    },
  });

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

  return (
    <div className="mx-auto max-w-2xl px-4 py-10">
      <div className="rounded-2xl border border-gray-200 bg-white p-8 shadow-sm">
        <div className="mb-8 flex items-center gap-6">
          {currentUser.avatarUrl ? (
            <img
              src={currentUser.avatarUrl}
              alt={currentUser.displayName}
              className="h-20 w-20 rounded-full object-cover ring-2 ring-teal-100"
            />
          ) : (
            <div className="flex h-20 w-20 items-center justify-center rounded-full bg-teal-100 text-2xl font-bold text-teal-600 ring-2 ring-teal-100">
              {(currentUser.displayName ?? currentUser.userName)
                .charAt(0)
                .toUpperCase()}
            </div>
          )}
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              {currentUser.displayName || currentUser.userName}
            </h1>
            <p className="mt-1 text-sm text-gray-500">@{currentUser.userName}</p>
            {!currentUser.emailConfirmed && (
              <span className="mt-2 inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-medium text-amber-800">
                Chưa xác nhận email
              </span>
            )}
          </div>
        </div>

        {!isEditing ? (
          <dl className="space-y-4">
            <div>
              <dt className="text-sm font-medium text-gray-500">Tên hiển thị</dt>
              <dd className="mt-1 text-sm text-gray-900">
                {currentUser.displayName || "—"}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Tên đăng nhập</dt>
              <dd className="mt-1 text-sm text-gray-900">{currentUser.userName}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Email</dt>
              <dd className="mt-1 text-sm text-gray-900">{currentUser.email}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Giới thiệu</dt>
              <dd className="mt-1 whitespace-pre-wrap text-sm text-gray-900">
                {currentUser.bio || "—"}
              </dd>
            </div>
          </dl>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
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
                Tên hiển thị
              </label>
              <input
                id="displayName"
                type="text"
                className={inputClasses}
                {...register("displayName")}
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
              />
              {errors.avatarUrl && (
                <p className="mt-1.5 text-sm text-red-600">
                  {errors.avatarUrl.message}
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
              />
              {errors.bio && (
                <p className="mt-1.5 text-sm text-red-600">{errors.bio.message}</p>
              )}
            </div>

            <div className="flex gap-3">
              <button
                type="submit"
                disabled={isSubmitting}
                className="rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700 disabled:opacity-60"
              >
                {isSubmitting ? "Đang lưu..." : "Lưu thay đổi"}
              </button>
              <button
                type="button"
                onClick={handleCancel}
                className="rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
              >
                Hủy
              </button>
            </div>
          </form>
        )}

        {!isEditing && (
          <div className="mt-8 flex gap-3 border-t border-gray-100 pt-6">
            <button
              type="button"
              onClick={() => setIsEditing(true)}
              className="rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700"
            >
              Chỉnh sửa hồ sơ
            </button>
            <button
              type="button"
              onClick={handleLogout}
              className="rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
            >
              Đăng xuất
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
