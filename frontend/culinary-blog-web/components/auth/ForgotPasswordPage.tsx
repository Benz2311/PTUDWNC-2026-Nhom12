"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useState } from "react";
import type { SubmitHandler } from "react-hook-form";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { useToast } from "@/components/ui/toast";
import { applyFieldErrors } from "@/components/auth/form-errors";
import { ApiError } from "@/lib/api/client";
import { authApi } from "@/lib/api/auth";

const forgotPasswordSchema = z.object({
  email: z.string().min(1, "Vui lòng nhập email").email("Email không hợp lệ"),
});

export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;

const inputClasses =
  "w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-100";
const errorInputClasses = "border-red-400 focus:border-red-400 focus:ring-red-100";
const labelClasses = "mb-1.5 block text-sm font-medium text-gray-700";

export default function ForgotPasswordPage() {
  const { success: showSuccessToast, error: showErrorToast } = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: "" },
  });

  const onSubmit: SubmitHandler<ForgotPasswordFormValues> = async (values) => {
    setIsSubmitting(true);
    try {
      await authApi.forgotPassword({ email: values.email });
      showSuccessToast(
        "Yêu cầu đã được gửi. Vui lòng kiểm tra email của bạn."
      );
    } catch (err) {
      if (err instanceof ApiError) {
        applyFieldErrors(err, setError);
        showErrorToast(err.title || "Gửi yêu cầu thất bại");
      } else {
        showErrorToast("Có lỗi xảy ra, vui lòng thử lại");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12">
      <div className="w-full max-w-md">
        <div className="rounded-2xl border border-gray-200 bg-white p-8 shadow-sm">
          <div className="mb-8 text-center">
            <h1 className="text-2xl font-bold text-gray-900">
              Khôi phục mật khẩu
            </h1>
            <p className="mt-2 text-sm text-gray-500">
              Vui lòng nhập email bạn đã sử dụng để đăng ký. Chúng tôi sẽ gửi cho
              bạn một liên kết để đặt lại mật khẩu.
            </p>
          </div>

          <form
            onSubmit={handleSubmit(onSubmit)}
            className="space-y-5"
            noValidate
          >
            <div>
              <label htmlFor="email" className={labelClasses}>
                Email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                className={`${inputClasses} ${
                  errors.email ? errorInputClasses : ""
                }`}
                placeholder="you@example.com"
                {...register("email")}
              />
              {errors.email && (
                <p className="mt-1.5 text-sm text-red-600">
                  {errors.email.message}
                </p>
              )}
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? "Đang gửi..." : "Gửi yêu cầu"}
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-gray-500">
            Đã có tài khoản?{" "}
            <Link
              href="/login"
              className="font-medium text-teal-600 hover:text-teal-700"
            >
              Quay lại đăng nhập
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
