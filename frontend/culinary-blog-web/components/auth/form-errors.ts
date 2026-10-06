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

  for (const [field, messages] of Object.entries(fieldErrors)) {
    const message = Array.isArray(messages) ? messages[0] : messages;
    if (message) {
      setError(field as FieldPath<TFieldValues>, {
        type: "server",
        message,
      });
    }
  }
}
