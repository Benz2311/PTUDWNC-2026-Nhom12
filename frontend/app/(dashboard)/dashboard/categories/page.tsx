import { getCategoryStatistics } from "@/lib/api/categories";
import CategoryStatistics from "@/components/category/CategoryStatistics";
import Link from "next/link";

export default async function DashboardCategoriesPage() {
  const statistics = await getCategoryStatistics();

  return (
    <main className="min-h-screen bg-gray-50 p-8">
      <div className="mx-auto max-w-6xl">
        <div className="mb-8 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">
              Thống kê danh mục
            </h1>
            <p className="mt-1 text-gray-500">
              Tổng quan về các danh mục và công thức món ăn
            </p>
          </div>

          <Link
            href="/categories"
            className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
          >
            Xem danh mục Public
          </Link>
        </div>

        <CategoryStatistics data={statistics} />
      </div>
    </main>
  );
}

