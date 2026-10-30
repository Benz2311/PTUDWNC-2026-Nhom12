import type { Metadata } from "next";
import ForgotPasswordPage from "@/components/auth/ForgotPasswordPage";

export const metadata: Metadata = {
  title: "Quên mật khẩu",
};

export default function ForgotPasswordRoute() {
  return <ForgotPasswordPage />;
}
