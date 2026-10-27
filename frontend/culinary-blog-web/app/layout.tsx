import type { Metadata } from "next";
import "./globals.css";
import { Header } from "@/components/layout/Header";
import { Footer } from "@/components/layout/Footer";

export const metadata: Metadata = {
  title: "Culinary Blog | Nền tảng chia sẻ và học nấu ăn trực tuyến",
  description:
    "Khám phá hàng ngàn công thức nấu ăn ngon, phong phú từ món Việt, món Á đến món Âu cùng hướng dẫn chi tiết và thông tin dinh dưỡng. Dự án PTUDWNC 2026 - Nhóm 12.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" className="scroll-smooth">
      <body className="min-h-screen flex flex-col bg-slate-50 text-slate-900 antialiased selection:bg-emerald-100 selection:text-emerald-900">
        {/* Skip to content link for accessibility */}
        <a
          href="#main-content"
          className="sr-only focus:not-sr-only focus:absolute focus:z-50 focus:p-4 focus:bg-emerald-700 focus:text-white"
        >
          Chuyển thẳng đến nội dung chính
        </a>

        {/* Global Navigation Header */}
        <Header />

        {/* Main Content Area */}
        <main id="main-content" className="flex-1 w-full">
          {children}
        </main>

        {/* Global Footer */}
        <Footer />
      </body>
    </html>
  );
}
