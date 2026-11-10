"use client";

import React from "react";
import Link from "next/link";
import {
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  MoreHorizontal,
} from "lucide-react";
import { cn } from "@/lib/utils";

export interface PaginationProps {
  page: number;
  totalPages: number;
  onPageChange?: (page: number) => void;
  buildLinkUrl?: (page: number) => string;
  siblingCount?: number;
  showFirstLast?: boolean;
  className?: string;
}

export function Pagination({
  page,
  totalPages,
  onPageChange,
  buildLinkUrl,
  siblingCount = 1,
  showFirstLast = true,
  className,
}: PaginationProps) {
  // If only 1 page or invalid totalPages, hide pagination
  if (totalPages <= 1) return null;

  // Generate page numbers range with ellipsis
  const generatePaginationItems = (): (number | "dots")[] => {
    const totalNumbers = siblingCount * 2 + 3; // siblings + current + first + last
    const totalBlocks = totalNumbers + 2; // + 2 dots

    if (totalPages <= totalBlocks) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    const leftSiblingIndex = Math.max(page - siblingCount, 1);
    const rightSiblingIndex = Math.min(page + siblingCount, totalPages);

    const shouldShowLeftDots = leftSiblingIndex > 2;
    const shouldShowRightDots = rightSiblingIndex < totalPages - 2;

    const firstPageIndex = 1;
    const lastPageIndex = totalPages;

    if (!shouldShowLeftDots && shouldShowRightDots) {
      const leftItemCount = 3 + 2 * siblingCount;
      const leftRange = Array.from({ length: leftItemCount }, (_, i) => i + 1);
      return [...leftRange, "dots", totalPages];
    }

    if (shouldShowLeftDots && !shouldShowRightDots) {
      const rightItemCount = 3 + 2 * siblingCount;
      const rightRange = Array.from(
        { length: rightItemCount },
        (_, i) => totalPages - rightItemCount + i + 1
      );
      return [firstPageIndex, "dots", ...rightRange];
    }

    if (shouldShowLeftDots && shouldShowRightDots) {
      const middleRange = Array.from(
        { length: rightSiblingIndex - leftSiblingIndex + 1 },
        (_, i) => leftSiblingIndex + i
      );
      return [firstPageIndex, "dots", ...middleRange, "dots", lastPageIndex];
    }

    return Array.from({ length: totalPages }, (_, i) => i + 1);
  };

  const paginationItems = generatePaginationItems();

  const handlePageClick = (e: React.MouseEvent, targetPage: number) => {
    if (targetPage === page || targetPage < 1 || targetPage > totalPages) {
      e.preventDefault();
      return;
    }
    if (onPageChange) {
      if (!buildLinkUrl) e.preventDefault();
      onPageChange(targetPage);
    }
  };

  const renderPageItem = (targetPage: number) => {
    const isActive = targetPage === page;
    const baseClasses = cn(
      "flex h-8 min-w-[2rem] sm:h-9 sm:min-w-[2.25rem] items-center justify-center rounded-lg px-2 sm:px-3 text-xs font-semibold transition-all cursor-pointer select-none",
      isActive
        ? "bg-emerald-700 text-white shadow-xs pointer-events-none cursor-default font-bold"
        : "text-gray-700 hover:bg-emerald-50 hover:text-emerald-800 border border-transparent hover:border-emerald-200"
    );

    const ariaLabel = isActive
      ? `Trang ${targetPage}, trang hiện tại`
      : `Đến trang ${targetPage}`;

    if (buildLinkUrl && !isActive) {
      return (
        <Link
          key={targetPage}
          href={buildLinkUrl(targetPage)}
          onClick={(e) => handlePageClick(e, targetPage)}
          aria-label={ariaLabel}
          aria-current={isActive ? "page" : undefined}
          className={baseClasses}
        >
          {targetPage}
        </Link>
      );
    }

    return (
      <button
        key={targetPage}
        type="button"
        onClick={(e) => handlePageClick(e, targetPage)}
        aria-label={ariaLabel}
        aria-current={isActive ? "page" : undefined}
        className={baseClasses}
      >
        {targetPage}
      </button>
    );
  };

  const renderNavButton = (
    targetPage: number,
    label: string,
    icon: React.ReactNode,
    isDisabled: boolean,
    extraClasses?: string
  ) => {
    const baseClasses = cn(
      "h-8 w-8 sm:h-9 sm:w-9 items-center justify-center rounded-lg border border-gray-200 text-gray-600 transition-colors select-none",
      isDisabled
        ? "pointer-events-none opacity-40 bg-gray-50 text-gray-400 cursor-not-allowed border-gray-200"
        : "hover:bg-emerald-50 hover:text-emerald-800 hover:border-emerald-200 cursor-pointer",
      extraClasses || "flex"
    );

    if (buildLinkUrl && !isDisabled) {
      return (
        <Link
          href={buildLinkUrl(targetPage)}
          onClick={(e) => handlePageClick(e, targetPage)}
          aria-label={label}
          className={baseClasses}
        >
          {icon}
        </Link>
      );
    }

    return (
      <button
        type="button"
        disabled={isDisabled}
        aria-disabled={isDisabled ? "true" : undefined}
        onClick={(e) => handlePageClick(e, targetPage)}
        aria-label={label}
        className={baseClasses}
      >
        {icon}
      </button>
    );
  };

  return (
    <nav
      role="navigation"
      aria-label="Pagination"
      className={cn("flex items-center justify-center gap-1 sm:gap-1.5 py-4 sm:py-6", className)}
    >
      {/* First Page Button - Ẩn trên mobile siêu nhỏ, hiện từ sm trở lên */}
      {showFirstLast &&
        renderNavButton(
          1,
          "Trang đầu tiên",
          <ChevronsLeft className="h-4 w-4" aria-hidden="true" />,
          page <= 1,
          "hidden sm:flex"
        )}

      {/* Previous Button */}
      {renderNavButton(
        page - 1,
        "Trang trước",
        <ChevronLeft className="h-4 w-4" aria-hidden="true" />,
        page <= 1,
        "flex"
      )}

      {/* Page Numbers */}
      <div className="flex items-center gap-1">
        {paginationItems.map((item, index) => {
          if (item === "dots") {
            return (
              <span
                key={`dots-${index}`}
                aria-hidden="true"
                className="flex h-8 w-6 sm:h-9 sm:w-8 items-center justify-center text-gray-400 select-none"
              >
                <MoreHorizontal className="h-4 w-4" />
              </span>
            );
          }
          return renderPageItem(item);
        })}
      </div>

      {/* Next Button */}
      {renderNavButton(
        page + 1,
        "Trang tiếp theo",
        <ChevronRight className="h-4 w-4" aria-hidden="true" />,
        page >= totalPages,
        "flex"
      )}

      {/* Last Page Button - Ẩn trên mobile siêu nhỏ, hiện từ sm trở lên */}
      {showFirstLast &&
        renderNavButton(
          totalPages,
          "Trang cuối cùng",
          <ChevronsRight className="h-4 w-4" aria-hidden="true" />,
          page >= totalPages,
          "hidden sm:flex"
        )}
    </nav>
  );
}

export default Pagination;
