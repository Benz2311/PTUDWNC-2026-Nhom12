import ProtectedRoute from "@/components/auth/ProtectedRoute";
import NutritionDashboard from "@/components/nutrition/NutritionDashboard";

export default function NutritionRoute() {
  return (
    <ProtectedRoute>
      <NutritionDashboard />
    </ProtectedRoute>
  );
}
