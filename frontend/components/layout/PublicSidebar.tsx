"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Layers,
  Sparkles,
  Flame,
  Clock,
  ArrowRight,
  BookmarkCheck,
  ChefHat,
  Search,
} from "lucide-react";
import { Skeleton } from "@/components/ui/Skeleton";
import { Badge } from "@/components/ui/Badge";
import { cn } from "@/lib/utils";

export interface SidebarCategoryItem {
  id: string;
  name: string;
  slug: string;
  recipeCount?: number;
  imageUrl?: string | null;
}

export interface PublicSidebarProps {
  categories?: SidebarCategoryItem[];
  activeCategorySlug?: string;
  isLoading?: boolean;
  error?: string | null;
  onRetry?: () => void;
  className?: string;
  showQuickLinks?: boolean;
}

export function PublicSidebar({
  categories = [],
  activeCategorySlug,
  isLoading = false,
  error = null,
  onRetry,
  className,
  showQuickLinks = true,
}: PublicSidebarProps) {
  const pathname = usePathname();

  return (
    <aside
      className={cn(
        "w-full space-y-6 lg:w-72 xl:w-80 flex-shrink-0",
        className
      )}
    >
      {/* 1. Category Navigation Box */}
      <div className="rounded-2xl border border-emerald-100 bg-white p-5 shadow-xs">
        <div className="flex items-center justify-between pb-3 border-b border-gray-100">
          <div className="flex items-center gap-2">
            <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700">
              <Layers className="h-4 w-4" />
            </div>
            <h3 className="text-sm font-bold text-gray-900 tracking-tight">
              Danh mục món ăn
            </h3>
          </div>
          <Link
            href="/categories"
            className="text-xs font-medium text-emerald-700 hover:text-emerald-800 hover:underline flex items-center gap-0.5"
          >
            Tất cả <ArrowRight className="h-3 w-3" />
          </Link>
        </div>

        {/* Loading state */}
        {isLoading && (
          <div className="mt-4 space-y-2.5">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="flex items-center justify-between py-1">
                <div className="flex items-center gap-2.5 flex-1">
                  <Skeleton className="h-4 w-4 rounded-full" />
                  <Skeleton className="h-4 w-3/4" />
                </div>
                <Skeleton className="h-4 w-6 rounded-full" />
              </div>
            ))}
          </div>
        )}

        {/* Error state */}
        {!isLoading && error && (
          <div className="mt-4 p-3 rounded-xl bg-red-50 text-center">
            <p className="text-xs text-red-600 mb-2">{error}</p>
            {onRetry && (
              <button
                onClick={onRetry}
                className="text-xs font-semibold text-red-700 underline hover:text-red-800"
              >
                Thử lại
              </button>
            )}
          </div>
        )}

        {/* Categories List */}
        {!isLoading && !error && categories.length > 0 && (
          <ul className="mt-3 space-y-1">
            {categories.map((cat) => {
              const isActive =
                activeCategorySlug === cat.slug ||
                pathname === `/categories/${cat.slug}`;
              return (
                <li key={cat.id}>
                  <Link
                    href={`/categories/${cat.slug}`}
                    className={cn(
                      "group flex items-center justify-between rounded-xl px-3 py-2 text-xs font-medium transition-all",
                      isActive
                        ? "bg-emerald-700 text-white shadow-xs font-semibold"
                        : "text-gray-700 hover:bg-emerald-50/80 hover:text-emerald-900"
                    )}
                  >
                    <span className="truncate flex items-center gap-2">
                      <span
                        className={cn(
                          "h-1.5 w-1.5 rounded-full transition-transform group-hover:scale-150",
                          isActive ? "bg-amber-400" : "bg-emerald-600"
                        )}
                      />
                      {cat.name}
                    </span>
                    {typeof cat.recipeCount === "number" && (
                      <span
                        className={cn(
                          "rounded-full px-2 py-0.5 text-[10px] font-semibold transition-colors",
                          isActive
                            ? "bg-emerald-800 text-emerald-100"
                            : "bg-gray-100 text-gray-600 group-hover:bg-emerald-100 group-hover:text-emerald-800"
                        )}
                      >
                        {cat.recipeCount}
                      </span>
                    )}
                  </Link>
                </li>
              );
            })}
          </ul>
        )}

        {/* Empty categories */}
        {!isLoading && !error && categories.length === 0 && (
          <p className="mt-4 text-center text-xs text-gray-500 py-3">
            Chưa có danh mục nào được hiển thị.
          </p>
        )}
      </div>

      {/* 2. Quick Discovery Links */}
      {showQuickLinks && (
        <div className="rounded-2xl border border-emerald-100 bg-white p-5 shadow-xs">
          <div className="flex items-center gap-2 pb-3 border-b border-gray-100">
            <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-amber-50 text-amber-700">
              <Sparkles className="h-4 w-4" />
            </div>
            <h3 className="text-sm font-bold text-gray-900 tracking-tight">
              Gợi ý khám phá
            </h3>
          </div>

          <div className="mt-3 space-y-1.5">
            <Link
              href="/recipes?sort=popular"
              className="flex items-center gap-3 rounded-xl px-3 py-2 text-xs font-medium text-gray-700 hover:bg-emerald-50 hover:text-emerald-800 transition-colors"
            >
              <Flame className="h-4 w-4 text-orange-500" />
              <span>Món ăn thịnh hành nhất</span>
            </Link>
            <Link
              href="/recipes?maxCookTimeMinutes=30"
              className="flex items-center gap-3 rounded-xl px-3 py-2 text-xs font-medium text-gray-700 hover:bg-emerald-50 hover:text-emerald-800 transition-colors"
            >
              <Clock className="h-4 w-4 text-blue-500" />
              <span>Nấu nhanh dưới 30 phút</span>
            </Link>
            <Link
              href="/recipes?difficulty=Easy"
              className="flex items-center gap-3 rounded-xl px-3 py-2 text-xs font-medium text-gray-700 hover:bg-emerald-50 hover:text-emerald-800 transition-colors"
            >
              <BookmarkCheck className="h-4 w-4 text-emerald-600" />
              <span>Dành cho người mới bắt đầu</span>
            </Link>
          </div>
        </div>
      )}

      {/* 3. Chef Inspiration Card */}
      <div className="rounded-2xl bg-gradient-to-br from-emerald-800 to-emerald-950 p-5 text-white shadow-md relative overflow-hidden">
        <div className="relative z-10 space-y-3">
          <div className="inline-flex items-center gap-1.5 rounded-full bg-white/10 px-2.5 py-1 text-[11px] font-semibold text-amber-300 backdrop-blur-xs">
            <ChefHat className="h-3.5 w-3.5" />
            <span>Mẹo làm bếp</span>
          </div>
          <h4 className="text-sm font-bold leading-snug">
            Bạn có công thức nấu ăn độc đáo của riêng mình?
          </h4>
          <p className="text-xs text-emerald-100/90 leading-relaxed">
            Hãy chia sẻ niềm đam mê ẩm thực với cộng đồng và truyền cảm hứng cho hàng ngàn người yêu bếp khác!
          </p>
          <Link
            href="/recipes/create"
            className="inline-flex items-center gap-1.5 rounded-lg bg-amber-400 px-3.5 py-1.5 text-xs font-bold text-gray-900 shadow-sm hover:bg-amber-300 transition-colors"
          >
            Đăng công thức ngay
          </Link>
        </div>
        <div className="absolute -bottom-6 -right-6 h-28 w-28 rounded-full bg-white/5 pointer-events-none" />
      </div>
    </aside>
  );
}

export default PublicSidebar;
