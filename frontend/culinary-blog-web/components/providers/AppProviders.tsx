import { AuthProvider } from "@/contexts/AuthContext";
import { ToastProvider, Toaster } from "@/components/ui/toast";
import Navbar from "@/components/layout/Navbar";

/** Gộp tất cả Providers chung của ứng dụng + Navbar toàn cục. */
export function AppProviders({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ToastProvider>
      <AuthProvider>
        <Navbar />
        {children}
        <Toaster />
      </AuthProvider>
    </ToastProvider>
  );
}
