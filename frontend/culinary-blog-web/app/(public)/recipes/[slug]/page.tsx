"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { getRecipeBySlug } from "@/lib/api/recipes";
import { extractErrorMessage } from "@/lib/api/client";
import type { RecipeDetail } from "@/types/recipe";

export default function RecipeDetailPage() {
  const params = useParams();
  const slug = params?.slug as string;

  const [recipe, setRecipe] = useState<RecipeDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"overview" | "ingredients" | "steps" | "nutrition" | "gallery">("overview");
  const [checkedIngredients, setCheckedIngredients] = useState<Record<string, boolean>>({});

  useEffect(() => {
    if (!slug) return;
    setLoading(true);
    setError(null);
    getRecipeBySlug(slug)
      .then((data) => {
        setRecipe(data);
        setLoading(false);
      })
      .catch((err) => {
        setLoading(false);
        setError(extractErrorMessage(err, "Không tìm thấy công thức hoặc bạn không có quyền xem."));
      });
  }, [slug]);

  const toggleIngredient = (id: string) => {
    setCheckedIngredients((prev) => ({
      ...prev,
      [id]: !prev[id],
    }));
  };

  if (loading) {
    return (
      <main className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-4xl animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 rounded w-1/4" />
          <div className="h-72 bg-gray-200 rounded-3xl" />
          <div className="h-6 bg-gray-200 rounded w-3/4" />
          <div className="h-32 bg-gray-200 rounded-2xl" />
        </div>
      </main>
    );
  }

  if (error || !recipe) {
    return (
      <main className="min-h-screen bg-gray-50 py-16 px-4">
        <div className="mx-auto max-w-md rounded-3xl bg-white p-8 text-center shadow-xs border border-gray-100 space-y-4">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-rose-50 text-2xl text-rose-600">
            ⚠️
          </div>
          <h2 className="text-lg font-bold text-gray-900">Không thể xem công thức</h2>
          <p className="text-xs text-gray-600">{error || "Công thức không tồn tại hoặc đã bị xóa."}</p>
          <div className="pt-2">
            <Link
              href="/recipes"
              className="inline-flex items-center rounded-xl bg-[#0d5c3a] px-4 py-2 text-xs font-semibold text-white hover:bg-[#094229]"
            >
              ← Quay lại danh sách công thức
            </Link>
          </div>
        </div>
      </main>
    );
  }

  // Sorted items
  const sortedIngredients = [...recipe.ingredients].sort((a, b) => a.orderIndex - b.orderIndex);
  const sortedSteps = [...recipe.steps].sort((a, b) => a.stepNumber - b.stepNumber);
  const sortedImages = [...recipe.images].sort((a, b) => a.orderIndex - b.orderIndex);

  // Primary image
  const primaryImg = sortedImages.find((img) => img.isPrimary) || sortedImages[0];
  const heroImageUrl = primaryImg?.originalUrl || "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=1200&q=80";

  return (
    <main className="min-h-screen bg-gray-50/60 pb-16">
      {/* ── BREADCRUMB ─────────────────────────────────────────────────── */}
      <div className="bg-white border-b border-gray-100">
        <div className="mx-auto max-w-5xl px-4 py-3 sm:px-6 lg:px-8 text-xs text-gray-500 flex items-center gap-2">
          <Link href="/" className="hover:text-[#0d5c3a]">Trang chủ</Link>
          <span>/</span>
          <Link href="/recipes" className="hover:text-[#0d5c3a]">Công thức</Link>
          <span>/</span>
          <Link href={`/categories/${recipe.category.slug}`} className="hover:text-[#0d5c3a]">
            {recipe.category.name}
          </Link>
          <span>/</span>
          <span className="text-gray-900 font-semibold truncate max-w-xs">{recipe.title}</span>
        </div>
      </div>

      <div className="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8 pt-8 space-y-8">
        {/* ── RECIPE HEADER & HERO (Screen 6 in Design) ──────────────────── */}
        <section className="overflow-hidden rounded-3xl bg-white border border-gray-100 shadow-xs">
          <div className="relative aspect-21/9 w-full overflow-hidden bg-gray-900">
            <img
              src={heroImageUrl}
              alt={recipe.title}
              className="h-full w-full object-cover object-center opacity-90"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/30 to-transparent" />

            {/* Overlay Info */}
            <div className="absolute bottom-6 left-6 right-6 text-white space-y-2">
              <div className="flex flex-wrap items-center gap-2">
                <Link
                  href={`/categories/${recipe.category.slug}`}
                  className="rounded-full bg-[#0d5c3a] px-3 py-1 text-xs font-bold text-white shadow-xs hover:bg-[#094229]"
                >
                  {recipe.category.name}
                </Link>
                <span className="rounded-full bg-white/20 backdrop-blur-xs px-2.5 py-0.5 text-xs font-semibold">
                  Độ khó: {recipe.difficulty}
                </span>
                <span className={`rounded-full px-2.5 py-0.5 text-xs font-semibold ${
                  recipe.status === "Published" ? "bg-emerald-500/80" : recipe.status === "Draft" ? "bg-slate-500/80" : "bg-amber-500/80"
                }`}>
                  {recipe.status === "Published" ? "Đã xuất bản" : recipe.status === "Draft" ? "Bản nháp" : "Lưu trữ"}
                </span>
              </div>

              <h1 className="text-2xl sm:text-4xl font-extrabold tracking-tight text-white">
                {recipe.title}
              </h1>

              {/* Author and Date */}
              <div className="flex items-center gap-3 pt-1 text-xs text-gray-200">
                <div className="flex items-center gap-1.5">
                  <span className="flex h-5 w-5 items-center justify-center rounded-full bg-emerald-600 text-white font-bold text-[10px]">
                    {recipe.author.displayName ? recipe.author.displayName[0].toUpperCase() : "A"}
                  </span>
                  <span className="font-semibold text-white">{recipe.author.displayName}</span>
                </div>
                <span>•</span>
                <span>{recipe.publishedAt ? new Date(recipe.publishedAt).toLocaleDateString("vi-VN") : "Bản nháp chưa xuất bản"}</span>
              </div>
            </div>
          </div>

          {/* Quick Metrics Bar */}
          <div className="grid grid-cols-2 sm:grid-cols-4 divide-x divide-gray-100 border-t border-gray-100 bg-white p-4 text-center">
            <div className="p-2">
              <span className="text-[11px] uppercase tracking-wider text-gray-400 font-semibold block">Chuẩn bị</span>
              <span className="text-base font-bold text-[#0d5c3a] mt-0.5 block">{recipe.prepTime} phút</span>
            </div>
            <div className="p-2">
              <span className="text-[11px] uppercase tracking-wider text-gray-400 font-semibold block">Nấu chín</span>
              <span className="text-base font-bold text-[#0d5c3a] mt-0.5 block">{recipe.cookTime} phút</span>
            </div>
            <div className="p-2">
              <span className="text-[11px] uppercase tracking-wider text-gray-400 font-semibold block">Khẩu phần</span>
              <span className="text-base font-bold text-[#0d5c3a] mt-0.5 block">{recipe.servings} người</span>
            </div>
            <div className="p-2">
              <span className="text-[11px] uppercase tracking-wider text-gray-400 font-semibold block">Tổng thời gian</span>
              <span className="text-base font-bold text-[#0d5c3a] mt-0.5 block">{recipe.prepTime + recipe.cookTime} phút</span>
            </div>
          </div>
        </section>

        {/* ── TABS NAVIGATION (Screen 6 in Design) ────────────────────────── */}
        <div className="flex border-b border-gray-200 bg-white rounded-2xl px-2 shadow-2xs overflow-x-auto no-scrollbar">
          {[
            { id: "overview", label: "Tổng quan", icon: "📋" },
            { id: "ingredients", label: `Nguyên liệu (${sortedIngredients.length})`, icon: "🥕" },
            { id: "steps", label: `Các bước nấu (${sortedSteps.length})`, icon: "🍳" },
            { id: "nutrition", label: "Dinh dưỡng", icon: "🥗" },
            { id: "gallery", label: `Hình ảnh (${sortedImages.length})`, icon: "📸" },
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as typeof activeTab)}
              className={`flex items-center gap-2 py-3 px-4 border-b-2 font-bold text-xs sm:text-sm transition-all whitespace-nowrap cursor-pointer ${
                activeTab === tab.id
                  ? "border-[#0d5c3a] text-[#0d5c3a]"
                  : "border-transparent text-gray-500 hover:text-gray-800"
              }`}
            >
              <span>{tab.icon}</span>
              <span>{tab.label}</span>
            </button>
          ))}
        </div>

        {/* ── TAB CONTENT ───────────────────────────────────────────────── */}
        <div className="rounded-3xl bg-white p-6 sm:p-8 border border-gray-100 shadow-xs">
          {/* 1. TỔNG QUAN */}
          {activeTab === "overview" && (
            <div className="space-y-6">
              <div>
                <h3 className="text-lg font-bold text-gray-900 mb-2">Giới thiệu món ăn</h3>
                <p className="text-sm text-gray-700 leading-relaxed whitespace-pre-line">
                  {recipe.description}
                </p>
              </div>

              {/* Highlights summary */}
              <div className="rounded-2xl bg-emerald-50/60 border border-emerald-100 p-5 space-y-3">
                <h4 className="text-xs font-bold uppercase tracking-wider text-[#0d5c3a]">Điểm nổi bật của món ăn</h4>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-xs text-gray-700">
                  <div className="flex items-center gap-2">
                    <span className="text-lg">⏱️</span>
                    <span>Nấu trong <strong>{recipe.cookTime} phút</strong></span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-lg">👥</span>
                    <span>Định lượng cho <strong>{recipe.servings} người ăn</strong></span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-lg">📊</span>
                    <span>Cấp độ: <strong>{recipe.difficulty}</strong></span>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* 2. NGUYÊN LIỆU (Sorted by OrderIndex) */}
          {activeTab === "ingredients" && (
            <div className="space-y-4">
              <div className="flex items-center justify-between pb-3 border-b border-gray-100">
                <h3 className="text-base font-bold text-gray-900">Danh sách nguyên liệu</h3>
                <span className="text-xs text-gray-500 italic">Đánh dấu vào ô khi bạn đã chuẩn bị xong</span>
              </div>

              {sortedIngredients.length === 0 ? (
                <p className="text-xs text-gray-400 italic">Chưa có nguyên liệu nào được thêm.</p>
              ) : (
                <div className="divide-y divide-gray-100">
                  {sortedIngredients.map((item, index) => {
                    const isChecked = !!checkedIngredients[item.id];
                    return (
                      <div
                        key={item.id}
                        onClick={() => toggleIngredient(item.id)}
                        className={`flex items-center justify-between py-3 px-2 rounded-xl transition-colors cursor-pointer ${
                          isChecked ? "bg-gray-50 line-through opacity-60 text-gray-400" : "hover:bg-emerald-50/40"
                        }`}
                      >
                        <div className="flex items-center gap-3">
                          <input
                            type="checkbox"
                            checked={isChecked}
                            onChange={() => toggleIngredient(item.id)}
                            className="h-4 w-4 rounded text-[#0d5c3a] focus:ring-[#0d5c3a]"
                          />
                          <div>
                            <span className="text-sm font-semibold text-gray-900">{item.name}</span>
                            {item.notes && (
                              <span className="block text-xs text-gray-500 italic">{item.notes}</span>
                            )}
                          </div>
                        </div>

                        <div className="text-xs font-bold text-[#0d5c3a] bg-emerald-50 px-2.5 py-1 rounded-lg">
                          {item.quantity !== null && item.quantity !== undefined
                            ? `${item.quantity} ${item.unit || ""}`
                            : "Vừa đủ"}
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          )}

          {/* 3. CÁC BƯỚC NẤU (Screen 7 in Design - Sorted by StepNumber) */}
          {activeTab === "steps" && (
            <div className="space-y-6">
              <div className="flex items-center justify-between pb-3 border-b border-gray-100">
                <h3 className="text-base font-bold text-gray-900">Quy trình thực hiện từng bước</h3>
                <span className="text-xs text-[#0d5c3a] font-semibold">Tổng cộng: {sortedSteps.length} bước</span>
              </div>

              {sortedSteps.length === 0 ? (
                <p className="text-xs text-gray-400 italic">Chưa có bước thực hiện nào được hướng dẫn.</p>
              ) : (
                <div className="space-y-6">
                  {sortedSteps.map((step) => (
                    <div
                      key={step.id}
                      className="flex gap-4 sm:gap-6 rounded-2xl border border-gray-100 bg-gray-50/40 p-4 sm:p-6 transition-all hover:border-emerald-200"
                    >
                      {/* Step Number Circle */}
                      <div className="shrink-0 flex h-10 w-10 sm:h-12 sm:w-12 items-center justify-center rounded-2xl bg-[#0d5c3a] text-white font-extrabold text-base shadow-xs">
                        {step.stepNumber}
                      </div>

                      {/* Step Details */}
                      <div className="flex-1 space-y-2">
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <h4 className="text-base font-bold text-gray-900">
                            {step.title || `Bước ${step.stepNumber}`}
                          </h4>
                          {step.timerMinutes && step.timerMinutes > 0 && (
                            <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-bold text-amber-800">
                              ⏱️ Hẹn giờ: {step.timerMinutes} phút
                            </span>
                          )}
                        </div>

                        <p className="text-sm text-gray-700 leading-relaxed whitespace-pre-line">
                          {step.description}
                        </p>

                        {/* Step Image */}
                        {step.imageUrl && (
                          <div className="pt-2">
                            <img
                              src={step.imageUrl}
                              alt={step.title || `Bước ${step.stepNumber}`}
                              className="max-h-60 rounded-xl object-cover border border-gray-200"
                            />
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* 4. DINH DƯỠNG */}
          {activeTab === "nutrition" && (
            <div className="space-y-6">
              <div className="flex items-center justify-between pb-3 border-b border-gray-100">
                <h3 className="text-base font-bold text-gray-900">Giá trị dinh dưỡng ước tính</h3>
                <span className="text-xs text-gray-500">Trên mỗi khẩu phần ăn</span>
              </div>

              {recipe.nutrition ? (
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
                  {[
                    { label: "Năng lượng (Calories)", value: recipe.nutrition.calories, unit: "kcal", icon: "🔥", color: "bg-orange-50 text-orange-800" },
                    { label: "Chất đạm (Protein)", value: recipe.nutrition.protein, unit: "g", icon: "🥩", color: "bg-emerald-50 text-emerald-800" },
                    { label: "Carbohydrate (Đường/Tinh bột)", value: recipe.nutrition.carbohydrates, unit: "g", icon: "🍞", color: "bg-amber-50 text-amber-800" },
                    { label: "Chất béo (Fat)", value: recipe.nutrition.fat, unit: "g", icon: "🧈", color: "bg-yellow-50 text-yellow-800" },
                    { label: "Chất xơ (Fiber)", value: recipe.nutrition.fiber, unit: "g", icon: "🥦", color: "bg-green-50 text-green-800" },
                    { label: "Natri (Sodium)", value: recipe.nutrition.sodium, unit: "mg", icon: "🧂", color: "bg-blue-50 text-blue-800" },
                  ].map((item) => (
                    <div key={item.label} className={`rounded-2xl p-4 border border-gray-100 ${item.color}`}>
                      <div className="flex items-center gap-2 text-xl mb-1">
                        <span>{item.icon}</span>
                      </div>
                      <span className="text-xs font-semibold block">{item.label}</span>
                      <span className="text-xl font-extrabold mt-1 block">
                        {item.value !== null && item.value !== undefined ? `${item.value} ${item.unit}` : "—"}
                      </span>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-xs text-gray-400 italic">Tác giả chưa cập nhật bảng dinh dưỡng cho công thức này.</p>
              )}

              {/* SRS Mandatory Disclaimer */}
              <div className="rounded-xl bg-gray-50 border border-gray-200 p-3.5 text-xs text-gray-500 flex items-center gap-2">
                <span className="text-base">ℹ️</span>
                <span>
                  <strong>Lưu ý:</strong> Thông tin dinh dưỡng do tác giả cung cấp và chỉ mang tính tham khảo.
                </span>
              </div>
            </div>
          )}

          {/* 5. HÌNH ẢNH (Sorted by OrderIndex) */}
          {activeTab === "gallery" && (
            <div className="space-y-4">
              <div className="flex items-center justify-between pb-3 border-b border-gray-100">
                <h3 className="text-base font-bold text-gray-900">Bộ sưu tập hình ảnh</h3>
                <span className="text-xs text-gray-500">{sortedImages.length} hình ảnh</span>
              </div>

              {sortedImages.length === 0 ? (
                <p className="text-xs text-gray-400 italic">Chưa có hình ảnh nào khác.</p>
              ) : (
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
                  {sortedImages.map((img) => (
                    <div
                      key={img.id}
                      className="group relative aspect-4/3 overflow-hidden rounded-2xl border border-gray-100 bg-gray-100 shadow-2xs"
                    >
                      <img
                        src={img.originalUrl}
                        alt={img.altText || recipe.title}
                        className="h-full w-full object-cover group-hover:scale-105 transition-transform"
                      />
                      {img.isPrimary && (
                        <span className="absolute top-2 left-2 rounded-full bg-[#0d5c3a] px-2.5 py-0.5 text-[10px] font-bold text-white shadow-xs">
                          Ảnh chính (Primary)
                        </span>
                      )}
                      {img.altText && (
                        <div className="absolute inset-x-0 bottom-0 bg-black/60 p-2 text-[11px] text-white opacity-0 group-hover:opacity-100 transition-opacity">
                          {img.altText}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </main>
  );
}
