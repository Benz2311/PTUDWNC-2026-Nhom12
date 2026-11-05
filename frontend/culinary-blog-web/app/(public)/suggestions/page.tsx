import ProtectedRoute from "@/components/auth/ProtectedRoute";
import IngredientSuggestionWidget from "@/components/ai/IngredientSuggestionWidget";

export default function SuggestionsRoute() {
  return (
    <ProtectedRoute>
      <IngredientSuggestionWidget />
    </ProtectedRoute>
  );
}
