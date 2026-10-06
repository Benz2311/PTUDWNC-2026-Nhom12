import Link from "next/link";
import { getCategoryBySlug } from "@/lib/api/categories";
import type { CategoryRecipeItem } from "@/types/category";

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
  const data = await getCategoryBySlug(slug);

  if (!data || !data.category) {
    return (
      <main className="min-h-screen bg-gray-50/60 py-16 px-4">
        <div className="mx-auto max-w-md rounded-3xl bg-white p-8 text-center shadow-xs border border-gray-100 space-y-4">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-rose-50 text-2xl text-rose-600">
            ⚠️
          </div>
          <h2 className="text-lg font-bold text-gray-900">Danh mục không tồn tại</h2>
          <p className="text-xs text-gray-600">
            Danh mục &ldquo;{slug}&rdquo; không tồn tại hoặc đã bị xóa.
          </p>
          <div className="pt-2">
            <Link
              href="/categories"
              className="inline-flex items-center rounded-xl bg-[#0d5c3a] px-4 py-2 text-xs font-semibold text-white hover:bg-[#094229]"
            >
              ← Quay lại danh mục
            </Link>
          </div>
        </div>
      </main>
    );
  }

  const { category, recipes } = data;

  return (
    <main className="min-h-screen bg-gray-50/60 py-10 px-4 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-6xl space-y-8">
        {/* Breadcrumb */}
        <div className="text-xs text-gray-500 flex items-center gap-2">
          <Link href="/" className="hover:text-[#0d5c3a]">Trang chủ</Link>
          <span>/</span>
          <Link href="/categories" className="hover:text-[#0d5c3a]">Danh mục</Link>
          <span>/</span>
          <span className="text-gray-900 font-semibold">{category.name}</span>
        </div>

        {/* Category Header Banner */}
        <div className="rounded-3xl bg-gradient-to-r from-[#0d5c3a] to-[#083a24] p-8 text-white shadow-xs">
          <div className="max-w-2xl space-y-3">
            <div className="flex items-center gap-2">
              <span className="rounded-full bg-white/20 px-3 py-0.5 text-xs font-bold">
                Danh mục món ăn
              </span>
              <span className="rounded-full bg-emerald-400/20 text-emerald-200 px-3 py-0.5 text-xs font-bold">
                {recipes.totalCount} công thức
              </span>
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

        {/* Recipes Grid */}
        <div>
          <div className="flex items-center justify-between mb-6 pb-3 border-b border-gray-100">
            <h2 className="text-lg font-bold text-gray-900">
              Công thức thuộc danh mục ({recipes.totalCount})
            </h2>
            <Link
              href="/recipes"
              className="text-xs font-semibold text-[#0d5c3a] hover:underline"
            >
              Xem tất cả công thức →
            </Link>
          </div>

          {recipes.items && recipes.items.length > 0 ? (
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {recipes.items.map((recipe: CategoryRecipeItem) => {
                const recipeImg =
                  recipe.primaryImageUrl ||
                  "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=600&q=80";

                return (
                  <Link
                    key={recipe.id}
                    href={`/recipes/${recipe.slug}`}
                    className="group flex flex-col justify-between overflow-hidden rounded-2xl bg-white border border-gray-100 shadow-xs hover:shadow-lg hover:border-emerald-200 transition-all duration-200"
                  >
                    <div className="aspect-4/3 w-full overflow-hidden bg-gray-100">
                      <img
                        src={recipeImg}
                        alt={recipe.title}
                        className="h-full w-full object-cover group-hover:scale-105 transition-transform duration-300"
                      />
                    </div>

                    <div className="flex flex-1 flex-col justify-between p-4">
                      <div>
                        <span className="inline-block rounded-full bg-emerald-50 px-2.5 py-0.5 text-[11px] font-bold text-[#0d5c3a] mb-2">
                          {category.name}
                        </span>
                        <h3 className="line-clamp-2 text-base font-bold text-gray-900 group-hover:text-[#0d5c3a] transition-colors">
                          {recipe.title}
                        </h3>
                        <p className="mt-1 line-clamp-2 text-xs text-gray-500 leading-relaxed">
                          {recipe.description}
                        </p>
                      </div>

                      <div className="mt-4 pt-3 border-t border-gray-100 flex items-center justify-between text-xs text-gray-500">
                        {recipe.cookTime ? (
                          <span className="flex items-center gap-1">
                            ⏱️ {recipe.cookTime} phút
                          </span>
                        ) : (
                          <span>Chuẩn vị</span>
                        )}
                        {recipe.difficulty && (
                          <span className="font-semibold text-emerald-800">
                            {recipe.difficulty}
                          </span>
                        )}
                      </div>
                    </div>
                  </Link>
                );
              })}
            </div>
          ) : (
            <div className="rounded-2xl bg-white border border-dashed border-gray-200 p-12 text-center space-y-3">
              <span className="text-3xl block">🍲</span>
              <p className="text-sm font-bold text-gray-800">Chưa có công thức nào trong danh mục này</p>
              <p className="text-xs text-gray-500">Hãy quay lại sau để cập nhật các công thức mới nhất.</p>
            </div>
          )}
        </div>
      </div>
    </main>
  );
}
