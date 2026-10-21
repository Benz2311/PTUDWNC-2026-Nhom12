"use client";

import React from "react";
import { PublicSidebar, type SidebarCategoryItem } from "./PublicSidebar";
import { cn } from "@/lib/utils";

export interface PublicLayoutProps {
  children: React.ReactNode;
  categories?: SidebarCategoryItem[];
  activeCategorySlug?: string;
  isLoadingCategories?: boolean;
  showSidebar?: boolean;
  sidebarPosition?: "left" | "right";
  className?: string;
}

export function PublicLayout({
  children,
  categories = [],
  activeCategorySlug,
  isLoadingCategories = false,
  showSidebar = true,
  sidebarPosition = "right",
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
          />
        )}

        {/* Primary Page Content */}
        <section className="flex-1 w-full min-w-0">
          {children}
        </section>

        {/* Right Sidebar (default) */}
        {showSidebar && sidebarPosition === "right" && (
          <PublicSidebar
            categories={categories}
            activeCategorySlug={activeCategorySlug}
            isLoading={isLoadingCategories}
          />
        )}
      </div>
    </div>
  );
}

export default PublicLayout;
