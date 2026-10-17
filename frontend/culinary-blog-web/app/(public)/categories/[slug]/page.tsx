import React from "react";
import Link from "next/link";
import { ArrowLeft, Clock, ChefHat, Sparkles } from "lucide-react";
import { getCategoryBySlug } from "@/lib/api/categories";
import type { CategoryRecipeItem } from "@/types/category";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { EmptyState } from "@/components/common/EmptyState";
import { ErrorState } from "@/components/common/ErrorState";

export const dynamic = "force-dynamic";

interface CategoryDetailPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function CategoryDetailPage({
  params,
}: CategoryDetailPageProps) {
  const { slug } = await params;
  const data = await getCategoryBySlug(slug).catch(() => null);

  if (!data || !data.category) {
    return (
      <div className="container-page py-16">
        <ErrorState
          title="Không tìm thấy danh mục"
          message={`Danh mục ẩm thực "${slug}" không tồn tại hoặc đã được chuyển sang địa chỉ khác.`}
          retryText="Xem tất cả danh mục"
        />
        <div className="text-center mt-4">
          <Link
            href="/categories"
            className="text-xs font-semibold text-emerald-700 hover:underline"
          >
            ← Quay lại danh sách danh mục
          </Link>
        </div>
      </div>
    );
  }

  const { category, recipes } = data;

  return (
    <div className="container-page py-10 space-y-8">
      {/* Breadcrumbs */}
      <nav aria-label="Breadcrumb" className="text-xs text-gray-500 flex items-center gap-2">
        <Link href="/" className="hover:text-emerald-700 transition-colors">
          Trang chủ
        </Link>
        <span>/</span>
        <Link href="/categories" className="hover:text-emerald-700 transition-colors">
          Danh mục
        </Link>
        <span>/</span>
        <span className="text-gray-900 font-semibold">{category.name}</span>
      </nav>

      {/* Category Header Banner */}
      <div className="rounded-3xl culinary-hero-gradient p-8 sm:p-10 text-white shadow-md relative overflow-hidden">
        <div className="max-w-2xl space-y-3 relative z-10">
          <div className="flex items-center gap-2">
            <span className="rounded-full bg-white/20 px-3 py-1 text-xs font-semibold backdrop-blur-xs">
              Danh mục món ăn
            </span>
            <Badge variant="secondary" className="bg-amber-400 text-gray-900 border-none font-bold">
              {recipes?.totalCount ?? 0} công thức
            </Badge>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight">
            {category.name}
          </h1>
          {category.description && (
            <p className="text-xs sm:text-sm text-emerald-100/90 leading-relaxed">
              {category.description}
            </p>
          )}
        </div>
      </div>

      {/* Recipes Section */}
      <div>
        <div className="flex items-center justify-between mb-6 pb-3 border-b border-gray-100">
          <h2 className="text-lg font-bold text-gray-900 flex items-center gap-2">
            <span>Công thức thuộc danh mục</span>
            <span className="text-xs text-emerald-800 bg-emerald-50 px-2 py-0.5 rounded-full font-semibold">
              {recipes?.totalCount ?? 0}
            </span>
          </h2>
          <Link
            href="/recipes"
            className="text-xs font-semibold text-emerald-700 hover:underline flex items-center gap-1"
          >
            <span>Tất cả công thức</span>
            <span>→</span>
          </Link>
        </div>

        {recipes && recipes.items && recipes.items.length > 0 ? (
          <div className="grid-recipes">
            {recipes.items.map((recipe: CategoryRecipeItem) => {
              const recipeImg =
                recipe.primaryImageUrl ||
                "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=600&q=80";

              return (
                <Link
                  key={recipe.id}
                  href={`/recipes/${recipe.slug}`}
                  className="group block"
                >
                  <Card className="overflow-hidden flex flex-col justify-between h-full culinary-card-hover border-gray-100">
                    <div className="aspect-4/3 w-full overflow-hidden bg-gray-100 relative">
                      <img
                        src={recipeImg}
                        alt={recipe.title}
                        className="h-full w-full object-cover group-hover:scale-105 transition-transform duration-300"
                        loading="lazy"
                      />
                      <div className="absolute top-3 left-3">
                        <Badge variant="secondary" className="bg-white/90 text-emerald-900 backdrop-blur-xs font-semibold shadow-xs">
                          {category.name}
                        </Badge>
                      </div>
                    </div>

                    <div className="flex flex-1 flex-col justify-between p-5 space-y-3">
                      <div>
                        <h3 className="line-clamp-2 text-base font-bold text-gray-900 group-hover:text-emerald-700 transition-colors">
                          {recipe.title}
                        </h3>
                        <p className="mt-1 line-clamp-2 text-xs text-gray-500 leading-relaxed">
                          {recipe.description}
                        </p>
                      </div>

                      <div className="pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-gray-500">
                        <span className="flex items-center gap-1">
                          <Clock className="h-3.5 w-3.5 text-gray-400" />
                          <span>{recipe.cookTime ? `${recipe.cookTime} phút` : "Chuẩn vị"}</span>
                        </span>
                        {recipe.difficulty && (
                          <Badge variant="outline" className="text-[11px] font-semibold text-emerald-800 border-emerald-200 bg-emerald-50/50">
                            {recipe.difficulty}
                          </Badge>
                        )}
                      </div>
                    </div>
                  </Card>
                </Link>
              );
            })}
          </div>
        ) : (
          <EmptyState
            icon={<ChefHat className="h-8 w-8" />}
            title="Chưa có công thức nào trong danh mục này"
            description="Các đầu bếp đang chuẩn bị những công thức ngon nhất cho danh mục này. Hãy quay lại sau nhé!"
            actionLabel="Xem các danh mục khác"
            actionHref="/categories"
          />
        )}
      </div>
    </div>
  );
}
