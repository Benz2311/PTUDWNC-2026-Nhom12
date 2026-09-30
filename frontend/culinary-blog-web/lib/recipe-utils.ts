/**
 * Utility function to format recipe cooking time for display.
 * @param prepMinutes - Preparation time in minutes
 * @param cookMinutes - Cooking time in minutes
 * @returns Formatted string like "45 phút" or "1 giờ 30 phút"
 */
export function formatTotalTime(prepMinutes: number, cookMinutes: number): string {
  const total = prepMinutes + cookMinutes;
  if (total <= 0) return "0 phút";
  const hours = Math.floor(total / 60);
  const minutes = total % 60;
  if (hours === 0) return `${minutes} phút`;
  if (minutes === 0) return `${hours} giờ`;
  return `${hours} giờ ${minutes} phút`;
}

/**
 * Generates a URL-safe slug from a Vietnamese or English string.
 */
export function generateSlug(text: string): string {
  return text
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/[\s-]+/g, "-")
    .trim();
}

/**
 * Truncates a string to the given max length, appending ellipsis if cut.
 */
export function truncate(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength).trimEnd() + "…";
}
