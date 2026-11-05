import ProtectedRoute from "@/components/auth/ProtectedRoute";
import FeedPage from "@/components/social/FeedPage";

export default function FeedRoute() {
  return (
    <ProtectedRoute>
      <FeedPage />
    </ProtectedRoute>
  );
}
