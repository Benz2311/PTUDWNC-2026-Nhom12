import type { Metadata } from "next";
import ProtectedRoute from "@/components/auth/ProtectedRoute";
import ProfilePage from "@/components/profile/ProfilePage";

export const metadata: Metadata = {
  title: "Hồ sơ của tôi",
};

export default function ProfileRoute() {
  return (
    <ProtectedRoute>
      <ProfilePage initialUser={null} />
    </ProtectedRoute>
  );
}
