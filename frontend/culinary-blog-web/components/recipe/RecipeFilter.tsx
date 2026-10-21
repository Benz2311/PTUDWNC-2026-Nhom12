"use client";

import React from "react";
import type { Category } from "@/types/category";
import type { RecipeFilterParams } from "@/types/recipe";
import { Filter, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/Button";

interface RecipeFilterProps {
  categories: Category[];
  filters: RecipeFilterParams;
  onFilterChange: (newFilters: RecipeFilterParams) => void;
  onReset: () => void;
  isAdmin?: boolean;
}

export default function RecipeFilter({
  categories,
  filters,
  onFilterChange,
  onReset,
  isAdmin = false,
}: RecipeFilterProps) {
  const handleCategoryChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    onFilterChange({
      ...filters,
      categorySlug: e.target.value || undefined,
      page: 1,
    });
  };

  const handleDifficultyChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    onFilterChange({
      ...filters,
      difficulty: e.target.value || undefined,
      page: 1,
    });
  };

  const handleMaxCookTimeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const val = e.target.value ? Number(e.target.value) : undefined;
    onFilterChange({
      ...filters,
      maxCookTimeMinutes: val,
      page: 1,
    });
  };

  const handleMineToggle = (e: React.ChangeEvent<HTMLInputElement>) => {
    onFilterChange({
      ...filters,
      mine: e.target.checked || undefined,
      page: 1,
    });
  };

  return (
    <aside className="w-full lg:w-64 shrink-0 rounded-2xl border border-gray-100 bg-white p-5 shadow-xs space-y-5">
      <div className="flex items-center justify-between pb-3 border-b border-gray-100">
        <div className="flex items-center gap-2 font-bold text-gray-900 text-sm">
          <Filter className="h-4 w-4 text-emerald-700" />
          <span>Bộ lọc tìm kiếm</span>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={onReset}
          className="text-xs text-gray-500 hover:text-rose-600 gap-1 h-7 px-2"
        >
          <RotateCcw className="h-3 w-3" />
          <span>Đặt lại</span>
        </Button>
      </div>

      {/* Category filter */}
      <div className="space-y-1.5">
        <label htmlFor="filter-category" className="text-xs font-semibold text-gray-700">
          Danh mục ẩm thực
        </label>
        <select
          id="filter-category"
          value={filters.categorySlug || ""}
          onChange={handleCategoryChange}
          className="w-full rounded-xl border border-gray-200 bg-gray-50 px-3 py-2 text-xs text-gray-800 outline-none focus:border-emerald-600 focus:bg-white"
        >
          <option value="">Tất cả danh mục</option>
          {categories.map((cat) => (
            <option key={cat.id} value={cat.slug}>
              {cat.name} {cat.recipeCount !== undefined ? `(${cat.recipeCount})` : ""}
            </option>
          ))}
        </select>
      </div>

      {/* Difficulty filter */}
      <div className="space-y-1.5">
        <label htmlFor="filter-difficulty" className="text-xs font-semibold text-gray-700">
          Độ khó thực hiện
        </label>
        <select
          id="filter-difficulty"
          value={filters.difficulty ? String(filters.difficulty) : ""}
          onChange={handleDifficultyChange}
          className="w-full rounded-xl border border-gray-200 bg-gray-50 px-3 py-2 text-xs text-gray-800 outline-none focus:border-emerald-600 focus:bg-white"
        >
          <option value="">Tất cả độ khó</option>
          <option value="Easy">Dễ (Easy)</option>
          <option value="Medium">Trung bình (Medium)</option>
          <option value="Hard">Khó (Hard)</option>
          <option value="Expert">Nâng cao (Expert)</option>
        </select>
      </div>

      {/* Cook time filter */}
      <div className="space-y-1.5">
        <label htmlFor="filter-cooktime" className="text-xs font-semibold text-gray-700">
          Thời gian nấu tối đa
        </label>
        <select
          id="filter-cooktime"
          value={filters.maxCookTimeMinutes ? String(filters.maxCookTimeMinutes) : ""}
          onChange={handleMaxCookTimeChange}
          className="w-full rounded-xl border border-gray-200 bg-gray-50 px-3 py-2 text-xs text-gray-800 outline-none focus:border-emerald-600 focus:bg-white"
        >
          <option value="">Không giới hạn</option>
          <option value="15">Dưới 15 phút</option>
          <option value="30">Dưới 30 phút</option>
          <option value="60">Dưới 1 giờ</option>
          <option value="120">Dưới 2 giờ</option>
        </select>
      </div>

      {/* Admin/mine filter */}
      {isAdmin && (
        <div className="pt-2 border-t border-gray-100">
          <label className="flex items-center gap-2 cursor-pointer text-xs font-medium text-gray-700">
            <input
              type="checkbox"
              checked={Boolean(filters.mine)}
              onChange={handleMineToggle}
              className="rounded border-gray-300 text-emerald-700 focus:ring-emerald-600"
            />
            <span>Chỉ xem công thức của tôi</span>
          </label>
        </div>
      )}
    </aside>
  );
}
