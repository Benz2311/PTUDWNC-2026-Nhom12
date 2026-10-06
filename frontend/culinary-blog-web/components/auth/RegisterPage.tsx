"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import { useState } from "react";
import type { SubmitHandler } from "react-hook-form";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Eye, EyeOff } from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/components/ui/toast";
import { applyFieldErrors } from "@/components/auth/form-errors";
import { ApiError } from "@/lib/api/client";
import { authApi } from "@/lib/api/auth";

const passwordRegex = /^(?=.*[A-Za-z])(?=.*\d)/;

const registerSchema = z
  .object({
    userName: z
      .string()
      .min(3, "Tên đăng nhập phải có ít nhất 3 ký tự")
      .max(32, "Tên đăng nhập không được quá 32 ký tự")
      .regex(/^[a-zA-Z0-9_]+$/, "Tên đăng nhập chỉ gồm chữ cái, số và dấu gạch dưới"),
    email: z.string().email("Email không hợp lệ"),
    password: z
      .string()
      .min(8, "Mật khẩu phải có ít nhất 8 ký tự")
      .regex(passwordRegex, "Mật khẩu phải chứa ít nhất 1 chữ cái và 1 chữ số"),
    confirmPassword: z.string(),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: "Xác nhận mật khẩu không khớp",
    path: ["confirmPassword"],
  });

export type RegisterFormValues = z.infer<typeof registerSchema>;

const inputClasses =
  "w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-100";
const errorInputClasses = "border-red-400 focus:border-red-400 focus:ring-red-100";
const labelClasses = "mb-1.5 block text-sm font-medium text-gray-700";

function extractErrorMessages(err: ApiError): string[] {
  const messages: string[] = [];
  const rawErrors = err.errors as unknown;

  if (Array.isArray(rawErrors)) {
    for (const item of rawErrors) {
      if (typeof item === "string" && item) {
        messages.push(item);
      } else if (
        item &&
        typeof item === "object" &&
        "message" in item &&
        typeof (item as { message: unknown }).message === "string"
      ) {
        messages.push((item as { message: string }).message);
      }
    }
  } else if (rawErrors && typeof rawErrors === "object") {
    for (const msgs of Object.values(
      rawErrors as Record<string, string[] | string>
    )) {
      if (Array.isArray(msgs)) {
        for (const m of msgs) {
          if (typeof m === "string" && m) messages.push(m);
        }
      } else if (typeof msgs === "string" && msgs) {
        messages.push(msgs);
      }
    }
  }

  if (messages.length === 0) {
    const detail = err.message;
    if (detail && detail !== "One or more validation errors occurred.") {
      messages.push(detail);
    }
  }

  const unique = Array.from(new Set(messages));
  return unique.length > 0 ? unique : ["Đăng ký thất bại"];
}

export default function RegisterPage() {
  const { register: registerAccount } = useAuth();
  const { error: showErrorToast, success, info } = useToast();
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formErrors, setFormErrors] = useState<string[]>([]);
  const [isResending, setIsResending] = useState(false);
  const [resendMessage, setResendMessage] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    setError,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { userName: "", email: "", password: "", confirmPassword: "" },
  });

  const watchedEmail = watch("email");
  const isEmail = watchedEmail.includes("@");

  const onSubmit: SubmitHandler<RegisterFormValues> = async (values) => {
    setIsSubmitting(true);
    setFormErrors([]);

    try {
      await registerAccount({
        userName: values.userName,
        email: values.email,
        password: values.password,
      });
      success("Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.");
      router.push("/profile");
      router.refresh();
    } catch (err) {
      if (err instanceof ApiError) {
        applyFieldErrors(err, setError);
        const messages = extractErrorMessages(err);
        setFormErrors(messages);
        showErrorToast(messages[0] || "Đăng ký thất bại");
      } else {
        setFormErrors(["Có lỗi xảy ra, vui lòng thử lại"]);
        showErrorToast("Có lỗi xảy ra, vui lòng thử lại");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleResendVerification = async () => {
    if (!isEmail) {
      return;
    }

    setIsResending(true);
    setResendMessage(null);

    try {
      await authApi.resendVerificationEmail(watchedEmail);
      setResendMessage("Đã gửi lại email xác nhận. Vui lòng kiểm tra hộp thư.");
      success("Đã gửi lại email xác nhận");
    } catch (err) {
      if (err instanceof ApiError) {
        setResendMessage(err.title || "Gửi lại email thất bại");
        showErrorToast(err.title || "Gửi lại email thất bại");
      } else {
        showErrorToast("Có lỗi xảy ra, vui lòng thử lại");
      }
    } finally {
      setIsResending(false);
    }
  };

  const handleGoogleRegister = () => {
    info("Đăng ký bằng Google đang được phát triển");
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12">
      <div className="w-full max-w-md">
        <div className="rounded-2xl border border-gray-200 bg-white p-8 shadow-sm">
          <div className="mb-8 text-center">
            <h1 className="text-2xl font-bold text-gray-900">
              Đăng ký tài khoản
            </h1>
            <p className="mt-2 text-sm text-gray-500">
              Tạo tài khoản để bắt đầu chia sẻ công thức nấu ăn
            </p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
            <div>
              <label htmlFor="userName" className={labelClasses}>
                Tên đăng nhập
              </label>
              <input
                id="userName"
                type="text"
                autoComplete="username"
                className={`${inputClasses} ${errors.userName ? errorInputClasses : ""}`}
                placeholder="nguyenvana"
                {...register("userName")}
              />
              {errors.userName && (
                <p className="mt-1.5 text-sm text-red-600">
                  {errors.userName.message}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="email" className={labelClasses}>
                Email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                className={`${inputClasses} ${errors.email ? errorInputClasses : ""}`}
                placeholder="you@example.com"
                {...register("email")}
              />
              {errors.email && (
                <p className="mt-1.5 text-sm text-red-600">
                  {errors.email.message}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="password" className={labelClasses}>
                Mật khẩu
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="new-password"
                  className={`${inputClasses} pr-10 ${errors.password ? errorInputClasses : ""}`}
                  placeholder="Ít nhất 8 ký tự, gồm chữ cái và số"
                  {...register("password")}
                />
                <button
                  type="button"
                  tabIndex={-1}
                  onClick={() => setShowPassword((v) => !v)}
                  aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                  aria-pressed={showPassword}
                  className="absolute right-3 top-1/2 -translate-y-1/2 rounded-md p-1 text-gray-500 hover:text-gray-700 focus:outline-none"
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1.5 text-sm text-red-600">
                  {errors.password.message}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="confirmPassword" className={labelClasses}>
                Xác nhận mật khẩu
              </label>
              <div className="relative">
                <input
                  id="confirmPassword"
                  type={showConfirmPassword ? "text" : "password"}
                  autoComplete="new-password"
                  className={`${inputClasses} pr-10 ${errors.confirmPassword ? errorInputClasses : ""}`}
                  placeholder="Nhập lại mật khẩu"
                  {...register("confirmPassword")}
                />
                <button
                  type="button"
                  tabIndex={-1}
                  onClick={() => setShowConfirmPassword((v) => !v)}
                  aria-label={showConfirmPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                  aria-pressed={showConfirmPassword}
                  className="absolute right-3 top-1/2 -translate-y-1/2 rounded-md p-1 text-gray-500 hover:text-gray-700 focus:outline-none"
                >
                  {showConfirmPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </button>
              </div>
              {errors.confirmPassword && (
                <p className="mt-1.5 text-sm text-red-600">
                  {errors.confirmPassword.message}
                </p>
              )}
            </div>

            {formErrors.length > 0 && (
              <div
                role="alert"
                className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
              >
                <ul className="list-disc pl-5">
                  {formErrors.map((message) => (
                    <li key={message}>{message}</li>
                  ))}
                </ul>
              </div>
            )}

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? "Đang đăng ký..." : "Đăng ký"}
            </button>
          </form>

          {isEmail && (
            <div className="mt-4 rounded-lg border border-sky-200 bg-sky-50 px-4 py-3">
              <p className="text-xs text-sky-800">
                Bạn đã nhập email. Nếu chưa nhận được email xác nhận:
              </p>
              <button
                type="button"
                onClick={handleResendVerification}
                disabled={isResending}
                className="mt-2 text-xs font-semibold text-sky-700 underline-offset-2 hover:underline disabled:opacity-60"
              >
                {isResending ? "Đang gửi lại..." : "Gửi lại email xác nhận"}
              </button>
              {resendMessage && (
                <p className="mt-1.5 text-xs text-sky-700">{resendMessage}</p>
              )}
            </div>
          )}

          <div className="my-6 flex items-center gap-4">
            <div className="h-px flex-1 bg-gray-200" />
            <span className="text-xs text-gray-400">hoặc</span>
            <div className="h-px flex-1 bg-gray-200" />
          </div>

          <button
            type="button"
            onClick={handleGoogleRegister}
            className="flex w-full items-center justify-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
          >
            <svg className="h-4 w-4" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.76h3.56c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
              <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.56-2.76c-.98.66-2.23 1.06-3.72 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84A11 11 0 0 0 12 23z" fill="#34A853" />
              <path d="M5.84 14.09A6.6 6.6 0 0 1 5.5 12c0-.73.13-1.44.34-2.09V7.07H2.18A11 11 0 0 0 1 12c0 1.77.42 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05" />
              <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1A11 11 0 0 0 2.18 7.07l3.66 2.84C6.71 7.31 9.14 5.38 12 5.38z" fill="#EA4335" />
            </svg>
            Đăng ký với Google
          </button>

          <p className="mt-6 text-center text-sm text-gray-500">
            Đã có tài khoản?{" "}
            <a href="/login" className="font-medium text-teal-600 hover:text-teal-700">
              Đăng nhập
            </a>
          </p>
        </div>
      </div>
    </div>
  );
}
