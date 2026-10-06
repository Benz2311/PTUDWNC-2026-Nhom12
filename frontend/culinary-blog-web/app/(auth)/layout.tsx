import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Xác thực",
};

export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <>{children}</>;
}
