import { getCategories } from "@/lib/api/categories";
import Link from "next/link";

export const dynamic = "force-dynamic";

export default async function CategoriesPage() {
  const categories = await getCategories();

  return (
    <main className="min-h-screen bg-gray-50 p-8">
      <div className="mx-auto max-w-6xl">
        <h1 className="mb-8 text-3xl font-bold">
          Danh mục món ăn
        </h1>

        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {categories.map((category) => (
            <Link
              key={category.id}
              href={`/categories/${category.slug}`}
              className="rounded-2xl bg-white p-6 shadow hover:shadow-lg"
            >
              <h2 className="text-xl font-bold">
                {category.name}
              </h2>

              <p className="mt-2 text-gray-600">
                {category.description}
              </p>

              <div className="mt-4 font-semibold">
                {category.recipeCount} công thức
              </div>
            </Link>
          ))}
        </div>
      </div>
    </main>
  );
}

