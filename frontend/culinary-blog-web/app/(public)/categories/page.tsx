import Link from "next/link";
import { getCategories } from "@/lib/api/categories";

export const dynamic = "force-dynamic";

export default async function CategoriesPage() {
  const categories = await getCategories();

  return (
    <main className="min-h-screen bg-gray-50/60 py-12 px-4 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-6xl space-y-8">
        {/* Header */}
        <div className="rounded-3xl bg-gradient-to-r from-[#0d5c3a] to-[#083a24] p-8 sm:p-10 text-white shadow-xs">
          <div className="max-w-2xl space-y-3">
            <span className="inline-block rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-emerald-100">
              Phân loại ẩm thực
            </span>
            <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight">
              Danh mục món ăn
            </h1>
            <p className="text-xs sm:text-sm text-emerald-100/90 leading-relaxed">
              Khám phá các nền văn hóa ẩm thực đặc sắc từ món Việt truyền thống, món Á đậm đà, món Âu tinh tế cho đến các món chay thanh đạm.
            </p>
          </div>
        </div>

        {/* Categories Grid */}
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {categories.map((category) => (
            <Link
              key={category.id}
              href={`/categories/${category.slug}`}
              className="group flex flex-col justify-between rounded-2xl bg-white p-6 border border-gray-100 shadow-xs hover:shadow-lg hover:border-emerald-200 transition-all duration-200"
            >
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-50 text-[#0d5c3a] font-extrabold text-lg group-hover:scale-105 group-hover:bg-[#0d5c3a] group-hover:text-white transition-all">
                    {category.name ? category.name[0] : "C"}
                  </div>
                  <span className="rounded-full bg-emerald-50 text-[#0d5c3a] px-3 py-1 text-xs font-bold">
                    {category.recipeCount} công thức
                  </span>
                </div>

                <div>
                  <h2 className="text-lg font-bold text-gray-900 group-hover:text-[#0d5c3a] transition-colors">
                    {category.name}
                  </h2>
                  <p className="mt-1.5 line-clamp-2 text-xs text-gray-500 leading-relaxed">
                    {category.description || "Khám phá các công thức hấp dẫn thuộc danh mục này."}
                  </p>
                </div>
              </div>

              <div className="mt-6 flex items-center gap-1 text-xs font-bold text-[#0d5c3a] group-hover:translate-x-1 transition-transform">
                <span>Xem danh sách công thức</span>
                <span>→</span>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </main>
  );
}
