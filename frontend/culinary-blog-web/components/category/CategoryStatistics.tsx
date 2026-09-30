import type { CategoryStatistics as ICategoryStatistics } from "@/types/category";

interface CategoryStatisticsProps {
  data: ICategoryStatistics;
}

export default function CategoryStatistics({ data }: CategoryStatisticsProps) {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <div className="rounded-xl bg-white p-5 shadow">
          <p className="text-sm font-medium text-gray-500">Tổng danh mục</p>
          <p className="mt-2 text-3xl font-bold text-gray-900">
            {data.totalCategories}
          </p>
        </div>

        <div className="rounded-xl bg-white p-5 shadow">
          <p className="text-sm font-medium text-gray-500">Tổng công thức</p>
          <p className="mt-2 text-3xl font-bold text-gray-900">
            {data.totalRecipes}
          </p>
        </div>

        <div className="rounded-xl bg-white p-5 shadow">
          <p className="text-sm font-medium text-gray-500">Đã xuất bản</p>
          <p className="mt-2 text-3xl font-bold text-green-600">
            {data.publishedRecipes}
          </p>
        </div>

        <div className="rounded-xl bg-white p-5 shadow">
          <p className="text-sm font-medium text-gray-500">Bản nháp</p>
          <p className="mt-2 text-3xl font-bold text-amber-600">
            {data.draftRecipes}
          </p>
        </div>
      </div>

      <div className="rounded-xl bg-white p-6 shadow">
        <h3 className="mb-4 text-lg font-bold text-gray-900">
          Số lượng công thức theo danh mục
        </h3>

        <div className="divide-y divide-gray-100">
          {data.categories.map((item) => (
            <div
              key={item.categoryId}
              className="flex items-center justify-between py-3"
            >
              <span className="font-medium text-gray-700">
                {item.categoryName}
              </span>
              <span className="rounded-full bg-blue-50 px-3 py-1 text-sm font-semibold text-blue-700">
                {item.recipeCount} công thức
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

