"use client";

import { useState } from "react";
import { z } from "zod";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/components/ui/toast";

const schema = z
  .object({
    currentPassword: z.string().min(1, "Vui lòng nhập mật khẩu hiện tại"),
    newPassword: z.string().min(8, "Mật khẩu mới phải có ít nhất 8 ký tự"),
    confirmPassword: z.string(),
  })
  .refine((value) => value.newPassword === value.confirmPassword, {
    path: ["confirmPassword"],
    message: "Xác nhận mật khẩu không khớp",
  });

export default function ChangePasswordCard() {
  const { changePassword } = useAuth();
  const { success, error } = useToast();
  const [values, setValues] = useState({ currentPassword: "", newPassword: "", confirmPassword: "" });
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const result = schema.safeParse(values);
    if (!result.success) {
      setMessage(result.error.issues[0]?.message ?? "Thông tin không hợp lệ");
      return;
    }
    setSubmitting(true);
    setMessage(null);
    try {
      await changePassword({ currentPassword: values.currentPassword, newPassword: values.newPassword });
      setValues({ currentPassword: "", newPassword: "", confirmPassword: "" });
      success("Đổi mật khẩu thành công");
    } catch (requestError) {
      const text = requestError instanceof ApiError
        ? requestError.title || requestError.message
        : "Không thể đổi mật khẩu.";
      error(text);
      setMessage(text);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <section className="mt-6 rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
      <h2 className="text-lg font-semibold text-gray-900">Đổi mật khẩu</h2>
      <form onSubmit={submit} className="mt-4 grid gap-4 md:grid-cols-3">
        {([
          ["currentPassword", "Mật khẩu hiện tại"],
          ["newPassword", "Mật khẩu mới"],
          ["confirmPassword", "Xác nhận mật khẩu mới"],
        ] as const).map(([name, label]) => (
          <label key={name} className="text-sm font-medium text-gray-700">
            {label}
            <input
              type="password"
              value={values[name]}
              onChange={(event) => setValues((current) => ({ ...current, [name]: event.target.value }))}
              className="mt-1.5 w-full rounded-lg border border-gray-300 px-3 py-2"
              autoComplete="new-password"
            />
          </label>
        ))}
        <div className="md:col-span-3">
          {message && <p role="alert" className="mb-3 text-sm text-red-600">{message}</p>}
          <button type="submit" disabled={submitting} className="rounded-lg bg-teal-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-60">
            {submitting ? "Đang cập nhật..." : "Cập nhật mật khẩu"}
          </button>
        </div>
      </form>
    </section>
  );
}
