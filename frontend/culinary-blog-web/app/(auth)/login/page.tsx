import type { Metadata } from "next";
import LoginPage from "@/components/auth/LoginPage";

export const metadata: Metadata = {
  title: "Đăng nhập",
};

export default function LoginRoute() {
  return <LoginPage />;
}
