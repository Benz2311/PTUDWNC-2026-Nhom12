import { AuthProvider } from "@/contexts/AuthContext";
import { ToastProvider, Toaster } from "@/components/ui/toast";

/** Gộp các providers dùng chung của ứng dụng. */
export function AppProviders({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ToastProvider>
      <AuthProvider>
        {children}
        <Toaster />
      </AuthProvider>
    </ToastProvider>
  );
}
