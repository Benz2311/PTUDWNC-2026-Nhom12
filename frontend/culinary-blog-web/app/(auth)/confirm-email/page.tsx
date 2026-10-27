import type { Metadata } from "next";
import ConfirmEmailPage from "@/components/auth/ConfirmEmailPage";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Xác thực email",
};

export default function ConfirmEmailRoute() {
  return <ConfirmEmailPage />;
}
