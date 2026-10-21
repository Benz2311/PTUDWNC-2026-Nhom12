import React from "react";
import { Skeleton } from "@/components/ui/Skeleton";
import { cn } from "@/lib/utils";

export interface LoadingSkeletonProps {
  variant?: "card" | "list" | "grid" | "detail" | "table";
  count?: number;
  className?: string;
}

export function LoadingSkeleton({
  variant = "grid",
  count = 6,
  className,
}: LoadingSkeletonProps) {
  if (variant === "card") {
    return <CardSkeleton className={className} />;
  }

  if (variant === "list") {
    return (
      <div className={cn("space-y-4 w-full", className)}>
        {Array.from({ length: count }).map((_, index) => (
          <div
            key={index}
            className="flex items-center space-x-4 p-4 rounded-xl border border-gray-100 bg-white"
          >
            <Skeleton className="h-16 w-16 rounded-lg flex-shrink-0" />
            <div className="space-y-2 flex-1">
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-3 w-1/2" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (variant === "detail") {
    return (
      <div className={cn("space-y-6 max-w-4xl mx-auto w-full", className)}>
        <Skeleton className="h-8 w-2/3" />
        <div className="flex gap-4">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-4 w-32" />
          <Skeleton className="h-4 w-20" />
        </div>
        <Skeleton className="h-80 w-full rounded-2xl" />
        <div className="space-y-3">
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-4/5" />
        </div>
      </div>
    );
  }

  if (variant === "table") {
    return (
      <div className={cn("w-full space-y-3", className)}>
        <Skeleton className="h-10 w-full rounded-lg" />
        {Array.from({ length: count }).map((_, index) => (
          <Skeleton key={index} className="h-12 w-full rounded-lg" />
        ))}
      </div>
    );
  }

  // default: grid of cards
  return (
    <div
      className={cn(
        "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 w-full",
        className
      )}
    >
      {Array.from({ length: count }).map((_, index) => (
        <CardSkeleton key={index} />
      ))}
    </div>
  );
}

export function CardSkeleton({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        "flex flex-col rounded-2xl border border-gray-100 bg-white overflow-hidden shadow-sm",
        className
      )}
    >
      <Skeleton className="h-48 w-full rounded-none" />
      <div className="p-5 space-y-3 flex-1 flex flex-col justify-between">
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Skeleton className="h-4 w-20 rounded-full" />
            <Skeleton className="h-4 w-12" />
          </div>
          <Skeleton className="h-5 w-4/5" />
          <Skeleton className="h-3 w-full" />
          <Skeleton className="h-3 w-2/3" />
        </div>
        <div className="pt-3 border-t border-gray-100 flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Skeleton className="h-7 w-7 rounded-full" />
            <Skeleton className="h-3 w-20" />
          </div>
          <Skeleton className="h-4 w-16" />
        </div>
      </div>
    </div>
  );
}
