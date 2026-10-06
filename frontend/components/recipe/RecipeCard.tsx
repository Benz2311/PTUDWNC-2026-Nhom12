import Link from "next/link";
import type { RecipeListItem } from "@/types/recipe";

interface RecipeCardProps {
  recipe: RecipeListItem;
  showStatus?: boolean;
}

export default function RecipeCard({ recipe, showStatus = false }: RecipeCardProps) {
  // Format difficulty badge
  const getDifficultyBadge = (difficulty: string) => {
    switch (difficulty?.toLowerCase()) {
      case "easy":
      case "dễ":
        return { label: "Dễ", bg: "bg-emerald-100 text-[#0d5c3a]" };
      case "medium":
      case "trung bình":
        return { label: "Trung bình", bg: "bg-amber-100 text-amber-800" };
      case "hard":
      case "khó":
        return { label: "Khó", bg: "bg-orange-100 text-orange-800" };
      case "expert":
      case "chuyên gia":
        return { label: "Chuyên gia", bg: "bg-rose-100 text-rose-800" };
      default:
        return { label: difficulty || "Trung bình", bg: "bg-gray-100 text-gray-700" };
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status?.toLowerCase()) {
      case "published":
      case "xuất bản":
        return { label: "Đã xuất bản", bg: "bg-emerald-600 text-white" };
      case "draft":
      case "nháp":
        return { label: "Bản nháp", bg: "bg-slate-500 text-white" };
      case "archived":
      case "lưu trữ":
        return { label: "Đã lưu trữ", bg: "bg-amber-600 text-white" };
      default:
        return { label: status, bg: "bg-gray-500 text-white" };
    }
  };

  const diffBadge = getDifficultyBadge(recipe.difficulty);
  const statusBadge = getStatusBadge(recipe.status);

  // Fallback image URL if primaryImageUrl is missing
  const imageUrl =
    recipe.primaryImageUrl && recipe.primaryImageUrl.trim().length > 0
      ? recipe.primaryImageUrl
      : "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=600&q=80";

  return (
    <Link
      href={`/recipes/${recipe.slug}`}
      className="group flex flex-col overflow-hidden rounded-2xl bg-white border border-gray-100 shadow-xs hover:shadow-lg hover:border-emerald-200 transition-all duration-200"
    >
      {/* Thumbnail with overlay badges */}
      <div className="relative aspect-4/3 w-full overflow-hidden bg-gray-100">
        <img
          src={imageUrl}
          alt={recipe.title}
          className="h-full w-full object-cover object-center group-hover:scale-105 transition-transform duration-300"
          loading="lazy"
        />
        {/* Category pill */}
        <div className="absolute top-3 left-3 flex flex-wrap gap-1.5">
          <span className="rounded-full bg-white/95 backdrop-blur-xs px-2.5 py-1 text-xs font-semibold text-[#0d5c3a] shadow-xs">
            {recipe.categoryName}
          </span>
          {showStatus && (
            <span className={`rounded-full px-2 py-0.5 text-xs font-medium shadow-xs ${statusBadge.bg}`}>
              {statusBadge.label}
            </span>
          )}
        </div>

        {/* Difficulty badge */}
        <div className="absolute top-3 right-3">
          <span className={`rounded-full px-2.5 py-1 text-xs font-bold shadow-xs ${diffBadge.bg}`}>
            {diffBadge.label}
          </span>
        </div>
      </div>

      {/* Content */}
      <div className="flex flex-1 flex-col justify-between p-4">
        <div>
          <h3 className="line-clamp-2 text-base font-bold text-gray-900 group-hover:text-[#0d5c3a] transition-colors">
            {recipe.title}
          </h3>
          <p className="mt-1.5 line-clamp-2 text-xs text-gray-500 leading-relaxed">
            {recipe.description}
          </p>
        </div>

        {/* Recipe Meta Info */}
        <div className="mt-4 pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-gray-600">
          {/* Cook time */}
          <div className="flex items-center gap-1">
            <svg className="w-4 h-4 text-emerald-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>{recipe.cookTime > 0 ? `${recipe.cookTime} phút` : `${recipe.totalTime} phút`}</span>
          </div>

          {/* Servings */}
          <div className="flex items-center gap-1">
            <svg className="w-4 h-4 text-emerald-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
            <span>{recipe.servings} người</span>
          </div>

          {/* Author */}
          <div className="flex items-center gap-1 max-w-[90px] truncate" title={recipe.authorName}>
            <span className="h-4 w-4 rounded-full bg-emerald-100 text-[#0d5c3a] flex items-center justify-center text-[9px] font-bold">
              {recipe.authorName ? recipe.authorName[0].toUpperCase() : "A"}
            </span>
            <span className="truncate">{recipe.authorName}</span>
          </div>
        </div>
      </div>
    </Link>
  );
}
