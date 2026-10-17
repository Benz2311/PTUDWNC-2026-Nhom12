import React from "react";
import { render, screen } from "@testing-library/react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { ErrorState } from "@/components/common/ErrorState";
import { EmptyState } from "@/components/common/EmptyState";
import { Pagination } from "@/components/common/Pagination";

describe("Shared UI Foundation Components", () => {
  it("renders Button correctly with text and handles loading state", () => {
    const { rerender } = render(<Button>Nấu ăn ngay</Button>);
    expect(screen.getByText("Nấu ăn ngay")).toBeInTheDocument();

    rerender(<Button isLoading>Đang lưu</Button>);
    expect(screen.getByRole("button")).toBeDisabled();
  });

  it("renders Input with placeholder and error message", () => {
    render(<Input placeholder="Nhập tên món ăn" error="Tên món không được để trống" />);
    expect(screen.getByPlaceholderText("Nhập tên món ăn")).toBeInTheDocument();
    expect(screen.getByText("Tên món không được để trống")).toBeInTheDocument();
  });

  it("renders Badge with appropriate variant", () => {
    render(<Badge variant="success">Hoàn thành</Badge>);
    expect(screen.getByText("Hoàn thành")).toBeInTheDocument();
  });

  it("renders LoadingSpinner with customized label", () => {
    render(<LoadingSpinner label="Đang tải danh mục..." />);
    expect(screen.getAllByText("Đang tải danh mục...")[0]).toBeInTheDocument();
  });

  it("renders ErrorState with title and message", () => {
    render(
      <ErrorState
        title="Lỗi kết nối"
        message="Không thể kết nối đến máy chủ"
        retryText="Thử lại"
        onRetry={() => {}}
      />
    );
    expect(screen.getByText("Lỗi kết nối")).toBeInTheDocument();
    expect(screen.getByText("Không thể kết nối đến máy chủ")).toBeInTheDocument();
  });

  it("renders EmptyState with action button", () => {
    render(
      <EmptyState
        title="Chưa có công thức"
        description="Hãy bắt đầu tạo công thức đầu tiên"
        actionLabel="Tạo mới"
        onAction={() => {}}
      />
    );
    expect(screen.getByText("Chưa có công thức")).toBeInTheDocument();
    expect(screen.getByText("Tạo mới")).toBeInTheDocument();
  });

  it("renders Pagination correctly with page numbers", () => {
    render(
      <Pagination
        page={2}
        totalPages={5}
        onPageChange={() => {}}
      />
    );
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.getByText("5")).toBeInTheDocument();
  });
});
