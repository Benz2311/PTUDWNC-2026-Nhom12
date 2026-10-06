"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import { useState } from "react";
import type { SubmitHandler } from "react-hook-form";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/components/ui/toast";
import { applyFieldErrors } from "@/components/auth/form-errors";
import { ApiError } from "@/lib/api/client";

const loginSchema = z.object({
  identifier: z
    .string()
    .min(1, "Vui lòng nhập email hoặc tên đăng nhập"),
  password: z.string().min(1, "Vui lòng nhập mật khẩu"),
});

export type LoginFormValues = z.infer<typeof loginSchema>;

const inputClasses =
  "w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-100";
const errorInputClasses = "border-red-400 focus:border-red-400 focus:ring-red-100";
const labelClasses = "mb-1.5 block text-sm font-medium text-gray-700";

export default function LoginPage() {
  const { login } = useAuth();
  const { error: showErrorToast, info } = useToast();
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { identifier: "", password: "" },
  });

  const onSubmit: SubmitHandler<LoginFormValues> = async (values) => {
    setIsSubmitting(true);
    setFormError(null);

    try {
      console.log("[LoginPage] Attempting login for:", values.identifier);
      await login({
        userNameOrEmail: values.identifier,
        password: values.password,
      });
      console.log("[LoginPage] Login successful, redirecting to home");
      router.push("/");
      router.refresh();
    } catch (err) {
      console.error("[LoginPage] Login error:", err);
      
      if (err instanceof ApiError) {
        console.log("[LoginPage] API Error details:", {
          status: err.status,
          title: err.title,
          message: err.message,
          errors: err.errors,
          traceId: err.traceId,
        });
        
        applyFieldErrors(err, setError);
        const errorMessage = err.title || err.message || "Đăng nhập thất bại";
        setFormError(errorMessage);
        showErrorToast(errorMessage);
      } else if (err instanceof Error) {
        console.error("[LoginPage] Unexpected error:", err.message, err.stack);
        setFormError("Có lỗi xảy ra, vui lòng thử lại");
        showErrorToast("Có lỗi xảy ra, vui lòng thử lại");
      } else {
        console.error("[LoginPage] Unknown error:", err);
        setFormError("Có lỗi xảy ra, vui lòng thử lại");
        showErrorToast("Có lỗi xảy ra, vui lòng thử lại");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleGoogleLogin = () => {
    info("Đăng nhập bằng Google đang được phát triển");
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12">
      <div className="w-full max-w-md">
        <div className="rounded-2xl border border-gray-200 bg-white p-8 shadow-sm">
          <div className="mb-8 text-center">
            <h1 className="text-2xl font-bold text-gray-900">
              Đăng nhập
            </h1>
            <p className="mt-2 text-sm text-gray-500">
              Chào mừng bạn trở lại với Culinary Blog
            </p>
          </div>

          {formError && (
            <div
              role="alert"
              className="mb-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
            >
              {formError}
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
            <div>
              <label htmlFor="identifier" className={labelClasses}>
                Email hoặc tên đăng nhập
              </label>
              <input
                id="identifier"
                type="text"
                autoComplete="username"
                className={`${inputClasses} ${errors.identifier ? errorInputClasses : ""}`}
                placeholder="you@example.com hoặc username"
                {...register("identifier")}
              />
              {errors.identifier && (
                <p className="mt-1.5 text-sm text-red-600">
                  {errors.identifier.message}
                </p>
              )}
            </div>

            <div>
              <div className="flex items-baseline justify-between">
                <label htmlFor="password" className={labelClasses}>
                  Mật khẩu
                </label>
                <a
                  href="/forgot-password"
                  className="text-sm font-medium text-teal-600 hover:text-teal-700"
                >
                  Quên mật khẩu?
                </a>
              </div>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                className={`${inputClasses} ${errors.password ? errorInputClasses : ""}`}
                placeholder="••••••••"
                {...register("password")}
              />
              {errors.password && (
                <p className="mt-1.5 text-sm text-red-600">
                  {errors.password.message}
                </p>
              )}
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? "Đang đăng nhập..." : "Đăng nhập"}
            </button>
          </form>

          <div className="my-6 flex items-center gap-4">
            <div className="h-px flex-1 bg-gray-200" />
            <span className="text-xs text-gray-400">hoặc</span>
            <div className="h-px flex-1 bg-gray-200" />
          </div>

          <button
            type="button"
            onClick={handleGoogleLogin}
            className="flex w-full items-center justify-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
          >
            <svg className="h-4 w-4" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.76h3.56c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
              <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.56-2.76c-.98.66-2.23 1.06-3.72 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84A11 11 0 0 0 12 23z" fill="#34A853" />
              <path d="M5.84 14.09A6.6 6.6 0 0 1 5.5 12c0-.73.13-1.44.34-2.09V7.07H2.18A11 11 0 0 0 1 12c0 1.77.42 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05" />
              <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1A11 11 0 0 0 2.18 7.07l3.66 2.84C6.71 7.31 9.14 5.38 12 5.38z" fill="#EA4335" />
            </svg>
            Đăng nhập với Google
          </button>

          <p className="mt-6 text-center text-sm text-gray-500">
            Chưa có tài khoản?{" "}
            <a href="/register" className="font-medium text-teal-600 hover:text-teal-700">
              Đăng ký ngay
            </a>
          </p>
        </div>
      </div>
    </div>
  );
}
