import type { Metadata } from "next";
import RegisterPage from "@/components/auth/RegisterPage";

export const metadata: Metadata = {
  title: "Đăng ký",
};

export default function RegisterRoute() {
  return <RegisterPage />;
}
