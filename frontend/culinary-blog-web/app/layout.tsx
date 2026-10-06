import type { Metadata } from "next";
import "./globals.css";
import Navbar from "@/components/layout/Navbar";
import Footer from "@/components/layout/Footer";

export const metadata: Metadata = {
  title: "Culinary Blog | Nền tảng chia sẻ và học nấu ăn",
  description: "Khám phá hàng ngàn công thức nấu ăn ngon, phong phú từ món Việt, món Á đến món Âu. Phân hệ phụ trách: Lê Thị Ánh Nhung (2312709).",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi">
      <body className="min-h-screen flex flex-col bg-[#f8fafc] text-gray-900 antialiased">
        <Navbar />
        <div className="flex-1">
          {children}
        </div>
        <Footer />
      </body>
    </html>
  );
}
