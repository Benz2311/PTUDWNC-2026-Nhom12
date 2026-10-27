"use client";

import React from "react";
import { PublicSidebar, type SidebarCategoryItem } from "./PublicSidebar";
import { cn } from "@/lib/utils";

export interface PublicLayoutProps {
  children: React.ReactNode;
  categories?: SidebarCategoryItem[];
  activeCategorySlug?: string;
  isLoadingCategories?: boolean;
  categoriesError?: string | null;
  onRetryCategories?: () => void;
  showSidebar?: boolean;
  sidebarPosition?: "left" | "right";
  className?: string;
}

export function PublicLayout({
  children,
  categories = [],
  activeCategorySlug,
  isLoadingCategories = false,
  categoriesError = null,
  onRetryCategories,
  showSidebar = true,
  sidebarPosition = "left",
  className,
}: PublicLayoutProps) {
  return (
    <div className={cn("mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-8", className)}>
      <div className="flex flex-col lg:flex-row gap-8 items-start">
        {/* Left Sidebar if selected */}
        {showSidebar && sidebarPosition === "left" && (
          <PublicSidebar
            categories={categories}
            activeCategorySlug={activeCategorySlug}
            isLoading={isLoadingCategories}
            error={categoriesError}
            onRetry={onRetryCategories}
          />
        )}

        {/* Primary Page Content */}
        <section className="flex-1 w-full min-w-0">
          {children}
        </section>

        {/* Right Sidebar */}
        {showSidebar && sidebarPosition === "right" && (
          <PublicSidebar
            categories={categories}
            activeCategorySlug={activeCategorySlug}
            isLoading={isLoadingCategories}
            error={categoriesError}
            onRetry={onRetryCategories}
          />
        )}
      </div>
    </div>
  );
}

export default PublicLayout;
