import { formatTotalTime, generateSlug, truncate, parseFractionOrDecimal } from "@/lib/recipe-utils";

describe("formatTotalTime", () => {
  it("returns '0 phút' for zero total time", () => {
    expect(formatTotalTime(0, 0)).toBe("0 phút");
  });

  it("returns minutes only when total < 60 mins", () => {
    expect(formatTotalTime(15, 30)).toBe("45 phút");
  });

  it("returns hours only when minutes = 0", () => {
    expect(formatTotalTime(30, 30)).toBe("1 giờ");
  });

  it("returns hours and minutes for mixed values", () => {
    expect(formatTotalTime(30, 60)).toBe("1 giờ 30 phút");
  });

  it("handles large values correctly", () => {
    expect(formatTotalTime(60, 120)).toBe("3 giờ");
  });
});

describe("generateSlug", () => {
  it("converts English text to slug", () => {
    expect(generateSlug("Hello World")).toBe("hello-world");
  });

  it("strips Vietnamese diacritics", () => {
    const result = generateSlug("Phở Bò");
    expect(result).toMatch(/^[a-z0-9-]+$/);
  });

  it("converts đ to d", () => {
    expect(generateSlug("Đặc Sản")).toContain("d");
  });

  it("collapses multiple spaces and hyphens", () => {
    expect(generateSlug("Hello   World")).toBe("hello-world");
  });
});

describe("truncate", () => {
  it("returns original text when within limit", () => {
    expect(truncate("Short text", 100)).toBe("Short text");
  });

  it("truncates and adds ellipsis when over limit", () => {
    const result = truncate("A very long description that should be truncated", 20);
    expect(result).toHaveLength(21); // 20 chars + ellipsis (…)
    expect(result).toMatch(/…$/);
  });

  it("returns exact length text unchanged", () => {
    const text = "Exactly ten";
    expect(truncate(text, 11)).toBe(text);
  });
});

describe("parseFractionOrDecimal", () => {
  it("parses pure integers correctly", () => {
    expect(parseFractionOrDecimal("500")).toBe(500);
    expect(parseFractionOrDecimal(250)).toBe(250);
  });

  it("parses decimals with dot or comma correctly", () => {
    expect(parseFractionOrDecimal("1.5")).toBe(1.5);
    expect(parseFractionOrDecimal("2,75")).toBe(2.75);
  });

  it("parses simple fractions correctly (SRS Appendix D UX requirement)", () => {
    expect(parseFractionOrDecimal("1/2")).toBe(0.5);
    expect(parseFractionOrDecimal("3/4")).toBe(0.75);
    expect(parseFractionOrDecimal("2/3")).toBe(0.667);
  });

  it("parses mixed fractions correctly (SRS Appendix D UX requirement)", () => {
    expect(parseFractionOrDecimal("1 1/2")).toBe(1.5);
    expect(parseFractionOrDecimal("2 3/4")).toBe(2.75);
  });

  it("handles whitespace gracefully", () => {
    expect(parseFractionOrDecimal("  1 / 2  ")).toBe(0.5);
    expect(parseFractionOrDecimal(" 3   1/4 ")).toBe(3.25);
  });

  it("returns null for non-positive or invalid input", () => {
    expect(parseFractionOrDecimal("0")).toBeNull();
    expect(parseFractionOrDecimal("-1")).toBeNull();
    expect(parseFractionOrDecimal("abc")).toBeNull();
    expect(parseFractionOrDecimal("1/0")).toBeNull();
    expect(parseFractionOrDecimal("")).toBeNull();
    expect(parseFractionOrDecimal(null)).toBeNull();
  });
});
