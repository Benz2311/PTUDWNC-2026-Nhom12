import type { FieldPath, FieldValues, UseFormSetError } from "react-hook-form";
import type { ApiError } from "@/lib/api/client";

/**
 * Ánh xạ lỗi validation chuẩn RFC 7807
 * (errors: { fieldName: [message, ...] }) vào react-hook-form setError.
 */
export function applyFieldErrors<TFieldValues extends FieldValues>(
  apiError: ApiError,
  setError: UseFormSetError<TFieldValues>
): void {
  const fieldErrors = apiError.errors;
  if (!fieldErrors) {
    return;
  }

  Object.entries(fieldErrors).forEach(([field, messages]) => {
    const message = messages[0];
    if (message) {
      setError(field as FieldPath<TFieldValues>, {
        type: "server",
        message,
      });
    }
  });
}
