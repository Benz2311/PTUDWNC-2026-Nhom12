import React from "react";
import Link from "next/link";
import { UtensilsCrossed } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

export interface EmptyStateProps {
  icon?: React.ReactNode;
  title?: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
  actionHref?: string;
  className?: string;
}

export function EmptyState({
  icon,
  title = "Không có dữ liệu",
  description = "Hiện tại chưa có nội dung nào được tìm thấy trong mục này.",
  actionLabel,
  onAction,
  actionHref,
  className,
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center p-10 text-center rounded-2xl border border-dashed border-gray-200 bg-white/60 my-6 max-w-lg mx-auto",
        className
      )}
    >
      <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50 text-emerald-700 mb-4 ring-8 ring-emerald-50/50">
        {icon ?? <UtensilsCrossed className="h-8 w-8" />}
      </div>
      <h3 className="text-lg font-bold text-gray-900 mb-1">{title}</h3>
      <p className="text-sm text-gray-500 mb-6 max-w-sm">{description}</p>
      {actionLabel && actionHref && (
        <Link href={actionHref}>
          <Button variant="default">{actionLabel}</Button>
        </Link>
      )}
      {actionLabel && !actionHref && onAction && (
        <Button onClick={onAction} variant="default">
          {actionLabel}
        </Button>
      )}
    </div>
  );
}
