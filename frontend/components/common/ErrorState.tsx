import React from "react";
import { AlertCircle, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

export interface ErrorStateProps {
  title?: string;
  message?: string;
  onRetry?: () => void;
  className?: string;
  retryText?: string;
}

export function ErrorState({
  title = "Đã xảy ra sự cố",
  message = "Không thể tải dữ liệu vào lúc này. Vui lòng thử lại sau.",
  onRetry,
  className,
  retryText = "Thử lại ngay",
}: ErrorStateProps) {
  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col items-center justify-center p-8 text-center rounded-2xl border border-red-100 bg-red-50/40 my-6 max-w-md mx-auto",
        className
      )}
    >
      <div className="flex h-14 w-14 items-center justify-center rounded-full bg-red-100 text-red-600 mb-4 shadow-sm">
        <AlertCircle className="h-7 w-7" />
      </div>
      <h3 className="text-lg font-bold text-gray-900 mb-1">{title}</h3>
      <p className="text-sm text-gray-600 mb-6 max-w-xs">{message}</p>
      {onRetry && (
        <Button
          onClick={onRetry}
          variant="outline"
          className="border-red-200 text-red-700 hover:bg-red-50 hover:text-red-800"
        >
          <RefreshCw className="h-4 w-4 mr-2" />
          {retryText}
        </Button>
      )}
    </div>
  );
}
