"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import { z } from "zod";
import { ApiError } from "@/lib/api/client";
import { authApi } from "@/lib/api/auth";
import { useToast } from "@/components/ui/toast";
import { Eye, EyeOff } from "lucide-react";

const schema = z
  .object({
    password: z.string().min(8, "Mật khẩu phải có ít nhất 8 ký tự"),
    confirmPassword: z.string(),
  })
  .refine((value) => value.password === value.confirmPassword, {
    path: ["confirmPassword"],
    message: "Xác nhận mật khẩu không khớp",
  });

export default function ResetPasswordPage() {
  const token = useSearchParams().get("token") ?? "";
  const { success, error } = useToast();
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const result = schema.safeParse({ password, confirmPassword });
    if (!result.success) {
      setMessage(result.error.issues[0]?.message ?? "Thông tin không hợp lệ");
      return;
    }
    if (!token) {
      setMessage("Liên kết đặt lại mật khẩu không hợp lệ.");
      return;
    }

    setSubmitting(true);
    setMessage(null);
    try {
      await authApi.resetPassword({ token, newPassword: password });
      success("Đặt lại mật khẩu thành công");
      setMessage("Mật khẩu đã được cập nhật. Bạn có thể đăng nhập lại.");
    } catch (requestError) {
      const text = requestError instanceof ApiError
        ? requestError.title || requestError.message
        : "Liên kết không hợp lệ hoặc đã hết hạn.";
      error(text);
      setMessage(text);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4 py-12">
      <div className="w-full max-w-md rounded-2xl border border-gray-200 bg-white p-8 shadow-sm">
        <h1 className="text-center text-2xl font-bold text-gray-900">Đặt lại mật khẩu</h1>
        <p className="mt-2 text-center text-sm text-gray-500">Tạo mật khẩu mới cho tài khoản của bạn.</p>
        <form onSubmit={submit} className="mt-8 space-y-5">
          <label className="block text-sm font-medium text-gray-700">
            Mật khẩu mới
            <div className="relative">
              <input
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                className="mt-1.5 w-full rounded-lg border border-gray-300 px-4 py-2.5 pr-11"
                autoComplete="new-password"
              />
              <button
                type="button"
                onClick={() => setShowPassword((visible) => !visible)}
                aria-label={showPassword ? "Ẩn mật khẩu mới" : "Hiện mật khẩu mới"}
                className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-gray-500 hover:text-teal-700"
              >
                {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
              </button>
            </div>
          </label>
          <label className="block text-sm font-medium text-gray-700">
            Xác nhận mật khẩu
            <div className="relative">
              <input
                type={showConfirmPassword ? "text" : "password"}
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                className="mt-1.5 w-full rounded-lg border border-gray-300 px-4 py-2.5 pr-11"
                autoComplete="new-password"
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword((visible) => !visible)}
                aria-label={showConfirmPassword ? "Ẩn xác nhận mật khẩu" : "Hiện xác nhận mật khẩu"}
                className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-gray-500 hover:text-teal-700"
              >
                {showConfirmPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
              </button>
            </div>
          </label>
          {message && <p role="alert" className="text-sm text-red-600">{message}</p>}
          <button
            type="submit"
            disabled={submitting}
            className="w-full rounded-lg bg-teal-600 px-4 py-2.5 font-semibold text-white disabled:opacity-60"
          >
            {submitting ? "Đang cập nhật..." : "Đặt lại mật khẩu"}
          </button>
        </form>
        <p className="mt-6 text-center text-sm text-gray-500">
          <Link href="/login" className="font-medium text-teal-600">Quay lại đăng nhập</Link>
        </p>
      </div>
    </div>
  );
}
