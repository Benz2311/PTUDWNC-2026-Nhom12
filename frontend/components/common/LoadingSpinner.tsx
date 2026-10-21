import React from "react";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

export interface LoadingSpinnerProps {
  size?: "sm" | "md" | "lg" | "xl";
  className?: string;
  label?: string;
}

export function LoadingSpinner({
  size = "md",
  className,
  label = "Đang tải dữ liệu...",
}: LoadingSpinnerProps) {
  const sizeClasses = {
    sm: "h-4 w-4",
    md: "h-6 w-6",
    lg: "h-10 w-10",
    xl: "h-14 w-14",
  };

  return (
    <div
      role="status"
      className={cn("flex flex-col items-center justify-center gap-3 p-4", className)}
    >
      <Loader2
        className={cn(
          "animate-spin text-emerald-700",
          sizeClasses[size]
        )}
      />
      {label && (
        <span className="text-sm font-medium text-gray-500">{label}</span>
      )}
      <span className="sr-only">{label}</span>
    </div>
  );
}
