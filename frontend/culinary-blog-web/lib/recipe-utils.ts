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

/**
 * Parses user input for ingredient quantity (fractions like "1/2", mixed like "1 1/2",
 * decimals like "1.5" or "1,5", or integers like "500") into a decimal number.
 * Returns null if the value is invalid or <= 0.
 */
export function parseFractionOrDecimal(input: string | number | null | undefined): number | null {
  if (input === null || input === undefined) return null;
  if (typeof input === "number") {
    return isNaN(input) || input <= 0 ? null : Math.round(input * 1000) / 1000;
  }

  const raw = String(input).trim().replace(",", ".");
  if (!raw) return null;

  // 1. Mixed fraction: "1 1/2", "2  3/4"
  const mixedMatch = raw.match(/^(\d+)\s+(\d+)\s*\/\s*(\d+)$/);
  if (mixedMatch) {
    const whole = parseInt(mixedMatch[1], 10);
    const num = parseInt(mixedMatch[2], 10);
    const den = parseInt(mixedMatch[3], 10);
    if (den === 0) return null;
    const result = whole + num / den;
    return result > 0 ? Math.round(result * 1000) / 1000 : null;
  }

  // 2. Simple fraction: "1/2", "3/4"
  const fractionMatch = raw.match(/^(\d+)\s*\/\s*(\d+)$/);
  if (fractionMatch) {
    const num = parseInt(fractionMatch[1], 10);
    const den = parseInt(fractionMatch[2], 10);
    if (den === 0) return null;
    const result = num / den;
    return result > 0 ? Math.round(result * 1000) / 1000 : null;
  }

  // 3. Decimal or pure integer: "500", "1.5"
  const numVal = parseFloat(raw);
  if (!isNaN(numVal) && isFinite(numVal) && numVal > 0) {
    return Math.round(numVal * 1000) / 1000;
  }

  return null;
}
