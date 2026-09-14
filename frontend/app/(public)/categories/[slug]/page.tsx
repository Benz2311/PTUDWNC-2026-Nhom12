import { getCategoryBySlug } from "@/lib/api/categories";
import type { CategoryRecipeItem } from "@/types/category";
import Link from "next/link";

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
      <main className="min-h-screen bg-gray-50 p-8">
        <div className="mx-auto max-w-6xl">
          <Link
            href="/categories"
            className="mb-4 inline-block text-blue-600 hover:underline"
          >
            &larr; Quay lại danh mục
          </Link>
          <h1 className="text-2xl font-bold">Danh mục: {slug}</h1>
          <p className="mt-4 text-gray-500">Chưa có dữ liệu chi tiết từ API.</p>
        </div>
      </main>
    );
  }

  const { category, recipes } = data;

  return (
    <main className="min-h-screen bg-gray-50 p-8">
      <div className="mx-auto max-w-6xl">
        <Link
          href="/categories"
          className="mb-4 inline-block text-blue-600 hover:underline"
        >
          &larr; Quay lại danh mục
        </Link>

        <h1 className="mb-2 text-3xl font-bold">{category.name}</h1>
        {category.description && (
          <p className="mb-6 text-gray-600">{category.description}</p>
        )}

        <div className="mb-4 text-lg font-semibold">
          Danh sách công thức ({recipes.totalCount} công thức)
        </div>

        {recipes.items && recipes.items.length > 0 ? (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {recipes.items.map((recipe: CategoryRecipeItem) => (
              <div
                key={recipe.id}
                className="rounded-2xl bg-white p-6 shadow hover:shadow-lg"
              >
                <h3 className="text-xl font-bold">{recipe.title}</h3>
                <p className="mt-2 text-gray-600">{recipe.description}</p>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-gray-500">Chưa có công thức nào trong danh mục này.</p>
        )}
      </div>
    </main>
  );
}

