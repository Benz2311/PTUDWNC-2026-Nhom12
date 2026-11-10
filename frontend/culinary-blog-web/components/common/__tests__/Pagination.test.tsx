import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { Pagination } from "../Pagination";

describe("Pagination Component (Unit & Accessibility Tests)", () => {
  it("returns null when totalPages <= 1", () => {
    const { container } = render(
      <Pagination page={1} totalPages={1} onPageChange={() => {}} />
    );
    expect(container.firstChild).toBeNull();
  });

  it("renders correctly on Page 1 (First & Previous disabled, Next & Last enabled)", () => {
    const handlePageChange = jest.fn();
    render(
      <Pagination page={1} totalPages={9} onPageChange={handlePageChange} />
    );

    const nav = screen.getByRole("navigation", { name: /pagination/i });
    expect(nav).toBeInTheDocument();

    // First and Previous buttons disabled on page 1
    const firstBtn = screen.getByRole("button", { name: "Trang đầu tiên" });
    const prevBtn = screen.getByRole("button", { name: "Trang trước" });
    expect(firstBtn).toBeDisabled();
    expect(prevBtn).toBeDisabled();

    // Page 1 is active with aria-current="page"
    const page1Btn = screen.getByRole("button", { name: /Trang 1, trang hiện tại/i });
    expect(page1Btn).toHaveAttribute("aria-current", "page");

    // Next and Last buttons enabled
    const nextBtn = screen.getByRole("button", { name: "Trang tiếp theo" });
    const lastBtn = screen.getByRole("button", { name: "Trang cuối cùng" });
    expect(nextBtn).not.toBeDisabled();
    expect(lastBtn).not.toBeDisabled();
  });

  it("renders correctly on Middle Page (all navigation buttons enabled)", () => {
    const handlePageChange = jest.fn();
    render(
      <Pagination page={4} totalPages={9} onPageChange={handlePageChange} />
    );

    const firstBtn = screen.getByRole("button", { name: "Trang đầu tiên" });
    const prevBtn = screen.getByRole("button", { name: "Trang trước" });
    const nextBtn = screen.getByRole("button", { name: "Trang tiếp theo" });
    const lastBtn = screen.getByRole("button", { name: "Trang cuối cùng" });

    expect(firstBtn).not.toBeDisabled();
    expect(prevBtn).not.toBeDisabled();
    expect(nextBtn).not.toBeDisabled();
    expect(lastBtn).not.toBeDisabled();

    // Page 4 is active
    const page4Btn = screen.getByRole("button", { name: /Trang 4, trang hiện tại/i });
    expect(page4Btn).toHaveAttribute("aria-current", "page");
  });

  it("renders correctly on Last Page (Next & Last disabled, First & Prev enabled)", () => {
    const handlePageChange = jest.fn();
    render(
      <Pagination page={9} totalPages={9} onPageChange={handlePageChange} />
    );

    const firstBtn = screen.getByRole("button", { name: "Trang đầu tiên" });
    const prevBtn = screen.getByRole("button", { name: "Trang trước" });
    const nextBtn = screen.getByRole("button", { name: "Trang tiếp theo" });
    const lastBtn = screen.getByRole("button", { name: "Trang cuối cùng" });

    expect(firstBtn).not.toBeDisabled();
    expect(prevBtn).not.toBeDisabled();
    expect(nextBtn).toBeDisabled();
    expect(lastBtn).toBeDisabled();

    const page9Btn = screen.getByRole("button", { name: /Trang 9, trang hiện tại/i });
    expect(page9Btn).toHaveAttribute("aria-current", "page");
  });

  it("calls onPageChange with correct page when clicking a page number or nav button", () => {
    const handlePageChange = jest.fn();
    render(
      <Pagination page={2} totalPages={9} onPageChange={handlePageChange} />
    );

    // Click on page 3
    const page3Btn = screen.getByRole("button", { name: "Đến trang 3" });
    fireEvent.click(page3Btn);
    expect(handlePageChange).toHaveBeenCalledWith(3);

    // Click on Next (page + 1 = 3)
    const nextBtn = screen.getByRole("button", { name: "Trang tiếp theo" });
    fireEvent.click(nextBtn);
    expect(handlePageChange).toHaveBeenCalledWith(3);

    // Click on Prev (page - 1 = 1)
    const prevBtn = screen.getByRole("button", { name: "Trang trước" });
    fireEvent.click(prevBtn);
    expect(handlePageChange).toHaveBeenCalledWith(1);

    // Click on First (1)
    const firstBtn = screen.getByRole("button", { name: "Trang đầu tiên" });
    fireEvent.click(firstBtn);
    expect(handlePageChange).toHaveBeenCalledWith(1);

    // Click on Last (9)
    const lastBtn = screen.getByRole("button", { name: "Trang cuối cùng" });
    fireEvent.click(lastBtn);
    expect(handlePageChange).toHaveBeenCalledWith(9);
  });

  it("does not call onPageChange when clicking active page", () => {
    const handlePageChange = jest.fn();
    render(
      <Pagination page={2} totalPages={9} onPageChange={handlePageChange} />
    );

    const page2Btn = screen.getByRole("button", { name: /Trang 2, trang hiện tại/i });
    fireEvent.click(page2Btn);
    expect(handlePageChange).not.toHaveBeenCalled();
  });

  it("renders ellipsis dots with aria-hidden='true'", () => {
    const { container } = render(
      <Pagination page={1} totalPages={9} onPageChange={() => {}} />
    );

    const hiddenDots = container.querySelectorAll("span[aria-hidden='true']");
    expect(hiddenDots.length).toBeGreaterThan(0);
  });

  it("applies responsive mobile classes (hidden sm:flex on First and Last buttons)", () => {
    render(
      <Pagination page={2} totalPages={9} onPageChange={() => {}} />
    );

    const firstBtn = screen.getByRole("button", { name: "Trang đầu tiên" });
    const lastBtn = screen.getByRole("button", { name: "Trang cuối cùng" });

    expect(firstBtn).toHaveClass("hidden");
    expect(firstBtn).toHaveClass("sm:flex");
    expect(lastBtn).toHaveClass("hidden");
    expect(lastBtn).toHaveClass("sm:flex");
  });

  it("renders links when buildLinkUrl is provided", () => {
    const buildLinkUrl = (p: number) => `/recipes?page=${p}`;
    render(
      <Pagination
        page={1}
        totalPages={5}
        buildLinkUrl={buildLinkUrl}
        onPageChange={() => {}}
      />
    );

    // Inactive pages should render as <a> links
    const page2Link = screen.getByRole("link", { name: "Đến trang 2" });
    expect(page2Link).toHaveAttribute("href", "/recipes?page=2");

    const nextLink = screen.getByRole("link", { name: "Trang tiếp theo" });
    expect(nextLink).toHaveAttribute("href", "/recipes?page=2");
  });
});
