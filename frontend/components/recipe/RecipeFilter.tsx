"use client";

import type { Category } from "@/types/category";
import type { RecipeFilterParams } from "@/types/recipe";

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
  return (
    <aside className="w-full lg:w-64 shrink-0 space-y-6 rounded-2xl bg-white p-5 border border-gray-100 shadow-xs">
      {/* Header */}
      <div className="flex items-center justify-between pb-3 border-b border-gray-100">
        <div className="flex items-center gap-2">
          <svg className="w-4 h-4 text-[#0d5c3a]" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
          </svg>
          <h3 className="text-sm font-bold text-gray-900">Lọc công thức</h3>
        </div>
        <button
          onClick={onReset}
          className="text-xs text-[#0d5c3a] hover:underline font-medium cursor-pointer"
        >
          Đặt lại
        </button>
      </div>

      {/* 1. Category Filter */}
      <div>
        <h4 className="text-xs font-bold text-gray-800 uppercase tracking-wider mb-2.5">
          Danh mục
        </h4>
        <div className="space-y-1.5 max-h-48 overflow-y-auto pr-1 text-xs">
          <label className="flex items-center gap-2 cursor-pointer py-1 px-1.5 rounded-lg hover:bg-emerald-50/50">
            <input
              type="radio"
              name="category"
              checked={!filters.categorySlug}
              onChange={() => onFilterChange({ ...filters, categorySlug: undefined, page: 1 })}
              className="text-[#0d5c3a] focus:ring-[#0d5c3a]"
            />
            <span className={!filters.categorySlug ? "font-bold text-[#0d5c3a]" : "text-gray-700"}>
              Tất cả danh mục
            </span>
          </label>
          {categories.map((cat) => (
            <label
              key={cat.id}
              className="flex items-center justify-between cursor-pointer py-1 px-1.5 rounded-lg hover:bg-emerald-50/50 text-xs"
            >
              <div className="flex items-center gap-2">
                <input
                  type="radio"
                  name="category"
                  checked={filters.categorySlug === cat.slug}
                  onChange={() => onFilterChange({ ...filters, categorySlug: cat.slug, page: 1 })}
                  className="text-[#0d5c3a] focus:ring-[#0d5c3a]"
                />
                <span className={filters.categorySlug === cat.slug ? "font-bold text-[#0d5c3a]" : "text-gray-700"}>
                  {cat.name}
                </span>
              </div>
              {cat.recipeCount !== undefined && (
                <span className="text-[10px] bg-gray-100 text-gray-600 px-1.5 py-0.5 rounded-full font-medium">
                  {cat.recipeCount}
                </span>
              )}
            </label>
          ))}
        </div>
      </div>

      {/* 2. Difficulty Filter */}
      <div>
        <h4 className="text-xs font-bold text-gray-800 uppercase tracking-wider mb-2.5">
          Độ khó
        </h4>
        <div className="grid grid-cols-2 gap-1.5 text-xs">
          {[
            { value: undefined, label: "Tất cả" },
            { value: "Easy", label: "Dễ" },
            { value: "Medium", label: "Trung bình" },
            { value: "Hard", label: "Khó" },
            { value: "Expert", label: "Chuyên gia" },
          ].map((item) => {
            const isSelected = filters.difficulty === item.value;
            return (
              <button
                key={item.label}
                type="button"
                onClick={() => onFilterChange({ ...filters, difficulty: item.value, page: 1 })}
                className={`py-1.5 px-2 rounded-lg border text-center transition-all ${
                  isSelected
                    ? "border-[#0d5c3a] bg-emerald-50 text-[#0d5c3a] font-bold shadow-2xs"
                    : "border-gray-200 text-gray-600 hover:bg-gray-50"
                }`}
              >
                {item.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* 3. Max Cook Time */}
      <div>
        <h4 className="text-xs font-bold text-gray-800 uppercase tracking-wider mb-2.5">
          Thời gian nấu tối đa
        </h4>
        <div className="flex flex-wrap gap-1.5 text-xs">
          {[
            { value: undefined, label: "Mọi thời gian" },
            { value: 30, label: "≤ 30 phút" },
            { value: 60, label: "≤ 60 phút" },
            { value: 120, label: "≤ 120 phút" },
          ].map((item) => {
            const isSelected = filters.maxCookTimeMinutes === item.value;
            return (
              <button
                key={item.label}
                type="button"
                onClick={() => onFilterChange({ ...filters, maxCookTimeMinutes: item.value, page: 1 })}
                className={`py-1 px-2.5 rounded-full border text-xs transition-all ${
                  isSelected
                    ? "border-[#0d5c3a] bg-[#0d5c3a] text-white font-medium shadow-2xs"
                    : "border-gray-200 text-gray-600 hover:bg-gray-50"
                }`}
              >
                {item.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* 4. Sắp xếp (Sort) */}
      <div>
        <h4 className="text-xs font-bold text-gray-800 uppercase tracking-wider mb-2.5">
          Sắp xếp theo
        </h4>
        <select
          value={`${filters.sortBy || "publishedAt"}_${filters.sortDirection || "desc"}`}
          onChange={(e) => {
            const [sortBy, sortDirection] = e.target.value.split("_") as [
              RecipeFilterParams["sortBy"],
              RecipeFilterParams["sortDirection"]
            ];
            onFilterChange({ ...filters, sortBy, sortDirection, page: 1 });
          }}
          className="w-full rounded-lg border border-gray-200 p-2 text-xs text-gray-800 bg-white focus:border-[#0d5c3a] focus:ring-1 focus:ring-[#0d5c3a] outline-none"
        >
          <option value="publishedAt_desc">Mới nhất (Ngày đăng giảm dần)</option>
          <option value="publishedAt_asc">Cũ nhất (Ngày đăng tăng dần)</option>
          <option value="title_asc">Tên công thức (A → Z)</option>
          <option value="title_desc">Tên công thức (Z → A)</option>
          <option value="cookTime_asc">Thời gian nấu (Nhanh nhất)</option>
          <option value="cookTime_desc">Thời gian nấu (Lâu nhất)</option>
        </select>
      </div>

      {/* 5. Author Scope (mine=true & Status Filter) */}
      <div className="pt-3 border-t border-gray-100">
        <h4 className="text-xs font-bold text-gray-800 uppercase tracking-wider mb-2">
          Quyền tác giả (Author)
        </h4>
        <label className="flex items-center gap-2 cursor-pointer text-xs text-gray-700">
          <input
            type="checkbox"
            checked={!!filters.mine}
            onChange={(e) => {
              const isMine = e.target.checked;
              onFilterChange({
                ...filters,
                mine: isMine ? true : undefined,
                status: isMine ? filters.status : undefined,
                page: 1,
              });
            }}
            className="rounded text-[#0d5c3a] focus:ring-[#0d5c3a]"
          />
          <span className="font-medium">Chỉ xem công thức của tôi (<code>mine=true</code>)</span>
        </label>

        {filters.mine && (
          <div className="mt-2 pl-5 space-y-1">
            <span className="text-[11px] text-gray-500 font-semibold block">Trạng thái công thức:</span>
            <select
              value={filters.status || ""}
              onChange={(e) =>
                onFilterChange({ ...filters, status: e.target.value || undefined, page: 1 })
              }
              className="w-full rounded-md border border-gray-200 p-1.5 text-xs text-gray-800 bg-white"
            >
              <option value="">Tất cả trạng thái</option>
              <option value="Draft">Bản nháp (Draft)</option>
              <option value="Published">Đã xuất bản (Published)</option>
              <option value="Archived">Đã lưu trữ (Archived)</option>
            </select>
          </div>
        )}
      </div>

      {/* 6. Admin Scope (Filter by authorId) */}
      {isAdmin && (
        <div className="pt-3 border-t border-purple-100 bg-purple-50/50 p-2.5 rounded-xl">
          <h4 className="text-xs font-bold text-purple-900 uppercase tracking-wider mb-1.5">
            Lọc theo Tác giả (Admin)
          </h4>
          <input
            type="text"
            placeholder="Nhập authorId GUID..."
            value={filters.authorId || ""}
            onChange={(e) =>
              onFilterChange({ ...filters, authorId: e.target.value.trim() || undefined, page: 1 })
            }
            className="w-full rounded-md border border-purple-200 p-1.5 text-xs font-mono bg-white outline-none focus:border-purple-600"
          />
        </div>
      )}
    </aside>
  );
}
