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
import GoogleLoginButton from "@/components/auth/GoogleLoginButton";

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
  const { login, googleLogin } = useAuth();
  const { error: showErrorToast } = useToast();
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

  const handleGoogleLogin = async (idToken: string) => {
    try {
      await googleLogin(idToken);
      router.push("/");
      router.refresh();
    } catch (err) {
      const message = err instanceof ApiError
        ? err.title || err.message
        : "Đăng nhập Google thất bại";
      showErrorToast(message);
    }
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

          <GoogleLoginButton onCredential={handleGoogleLogin} />

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
