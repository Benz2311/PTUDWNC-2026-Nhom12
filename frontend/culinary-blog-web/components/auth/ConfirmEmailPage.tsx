"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { CheckCircle2, Send, XCircle } from "lucide-react";
import { authApi } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

type VerifyState = "verifying" | "success" | "error" | "invalid";

const inputClasses =
  "w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-100";

export default function ConfirmEmailPage() {
  const searchParams = useSearchParams();
  const userId = searchParams.get("userId");
  const token = searchParams.get("token");

  const [state, setState] = useState<VerifyState>("verifying");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [resendEmail, setResendEmail] = useState("");
  const [isResending, setIsResending] = useState(false);
  const [resendMessage, setResendMessage] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      if (!userId || !token) {
        if (!cancelled) {
          setState("invalid");
        }
        return;
      }

      try {
        await authApi.confirmEmail(userId, token);
        if (!cancelled) {
          setState("success");
        }
      } catch (err) {
        if (cancelled) {
          return;
        }
        setState("error");
        setErrorMessage(
          err instanceof ApiError
            ? err.title || "Xác thực email thất bại"
            : "Có lỗi xảy ra, vui lòng thử lại"
        );
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [userId, token]);

  const handleResend = async () => {
    if (!resendEmail.includes("@")) {
      setResendMessage("Vui lòng nhập email đã đăng ký.");
      return;
    }

    setIsResending(true);
    setResendMessage(null);

    try {
      await authApi.resendVerificationEmail(resendEmail);
      setResendMessage("Đã gửi lại email xác nhận. Vui lòng kiểm tra hộp thư.");
    } catch (err) {
      setResendMessage(
        err instanceof ApiError
          ? err.title || "Gửi lại email thất bại"
          : "Có lỗi xảy ra, vui lòng thử lại"
      );
    } finally {
      setIsResending(false);
    }
  };

  return (
    <div className="flex min-h-[60vh] items-center justify-center bg-gray-50 px-4 py-12">
      <div className="w-full max-w-md rounded-2xl border border-gray-200 bg-white p-8 text-center shadow-sm">
        {state === "verifying" && (
          <>
            <div className="mx-auto h-12 w-12 animate-spin rounded-full border-4 border-gray-200 border-t-teal-600" />
            <h1 className="mt-6 text-xl font-semibold text-gray-900">
              Đang xác thực email...
            </h1>
            <p className="mt-2 text-sm text-gray-500">
              Vui lòng đợi trong giây lát.
            </p>
          </>
        )}

        {state === "success" && (
          <>
            <CheckCircle2 className="mx-auto h-14 w-14 text-teal-600" />
            <h1 className="mt-4 text-xl font-semibold text-gray-900">
              Email đã được xác nhận
            </h1>
            <p className="mt-2 text-sm text-gray-500">
              Tài khoản của bạn đã sẵn sàng. Hãy đăng nhập để bắt đầu.
            </p>
            <Link
              href="/login"
              className="mt-6 inline-block rounded-lg bg-teal-600 px-6 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700"
            >
              Đến trang Đăng nhập
            </Link>
          </>
        )}

        {(state === "error" || state === "invalid") && (
          <>
            <XCircle className="mx-auto h-14 w-14 text-red-500" />
            <h1 className="mt-4 text-xl font-semibold text-gray-900">
              {state === "invalid"
                ? "Liên kết không hợp lệ"
                : "Xác thực email thất bại"}
            </h1>
            <p className="mt-2 text-sm text-gray-500">
              {state === "invalid"
                ? "Liên kết xác thực thiếu thông tin. Vui lòng sử dụng link trong email xác nhận."
                : errorMessage}
            </p>

            <div className="mt-6 rounded-lg border border-gray-200 bg-gray-50 p-4">
              <p className="text-sm font-medium text-gray-700">
                Gửi lại email xác thực
              </p>
              <div className="mt-3 flex gap-2">
                <input
                  type="email"
                  value={resendEmail}
                  onChange={(event) => setResendEmail(event.target.value)}
                  placeholder="you@example.com"
                  aria-label="Email để gửi lại xác thực"
                  className={inputClasses}
                />
                <button
                  type="button"
                  onClick={handleResend}
                  disabled={isResending}
                  className="flex shrink-0 items-center gap-1.5 rounded-lg bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-teal-700 disabled:opacity-60"
                >
                  <Send className="h-4 w-4" />
                  {isResending ? "..." : "Gửi"}
                </button>
              </div>
              {resendMessage && (
                <p className="mt-2 text-xs text-gray-600">{resendMessage}</p>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
