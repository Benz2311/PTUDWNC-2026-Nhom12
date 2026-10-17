import React from "react";
import Link from "next/link";
import { Layers, ArrowRight, Utensils } from "lucide-react";
import { getCategories } from "@/lib/api/categories";
import { Card } from "@/components/ui/Card";
import { EmptyState } from "@/components/common/EmptyState";

export const dynamic = "force-dynamic";

export default async function CategoriesPage() {
  const categories = await getCategories().catch(() => []);

  return (
    <div className="container-page py-10 space-y-8">
      {/* Header Banner */}
      <div className="rounded-3xl culinary-hero-gradient p-8 sm:p-10 text-white shadow-md relative overflow-hidden">
        <div className="max-w-2xl space-y-3 relative z-10">
          <div className="inline-flex items-center gap-1.5 rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-emerald-100 backdrop-blur-xs">
            <Layers className="h-3.5 w-3.5 text-amber-400" />
            <span>Phân loại ẩm thực</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight">
            Danh mục món ăn
          </h1>
          <p className="text-xs sm:text-sm text-emerald-100/90 leading-relaxed">
            Khám phá các nền văn hóa ẩm thực đặc sắc từ món Việt truyền thống, món Á đậm đà, món Âu tinh tế cho đến các món tráng miệng và đồ uống thanh mát.
          </p>
        </div>
      </div>

      {/* Categories Grid */}
      {categories.length > 0 ? (
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {categories.map((category) => (
            <Link
              key={category.id}
              href={`/categories/${category.slug}`}
              className="group block"
            >
              <Card className="p-6 h-full flex flex-col justify-between culinary-card-hover border-gray-100">
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-800 font-extrabold text-lg group-hover:bg-emerald-700 group-hover:text-white transition-all shadow-xs">
                      {category.name ? category.name[0] : "C"}
                    </div>
                    {typeof category.recipeCount === "number" && (
                      <span className="rounded-full bg-emerald-50 text-emerald-800 px-3 py-1 text-xs font-bold border border-emerald-100">
                        {category.recipeCount} công thức
                      </span>
                    )}
                  </div>

                  <div>
                    <h2 className="text-lg font-bold text-gray-900 group-hover:text-emerald-800 transition-colors">
                      {category.name}
                    </h2>
                    <p className="mt-1.5 line-clamp-2 text-xs text-gray-500 leading-relaxed">
                      {category.description || "Khám phá các công thức hấp dẫn và cách chế biến thuộc danh mục này."}
                    </p>
                  </div>
                </div>

                <div className="mt-6 pt-4 border-t border-gray-50 flex items-center justify-between text-xs font-bold text-emerald-700 group-hover:translate-x-1 transition-transform">
                  <span>Khám phá ngay</span>
                  <ArrowRight className="h-4 w-4" />
                </div>
              </Card>
            </Link>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<Utensils className="h-8 w-8" />}
          title="Chưa có danh mục nào"
          description="Hiện tại hệ thống đang cập nhật các danh mục ẩm thực mới. Vui lòng quay lại sau."
          actionLabel="Về trang chủ"
          actionHref="/"
        />
      )}
    </div>
  );
}
