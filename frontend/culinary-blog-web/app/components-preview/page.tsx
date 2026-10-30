"use client";

import React, { useState } from "react";
import { RecipeDetailView } from "@/components/recipe/RecipeDetailView";
import RecipeFormLayout from "@/components/recipe/RecipeFormLayout";
import { StepEditor } from "@/components/recipe-management/StepEditor";
import { ImageManager } from "@/components/recipe-management/ImageManager";
import { ConfirmDialog } from "@/components/common/ConfirmDialog";
import { EmptyState } from "@/components/common/EmptyState";
import { ErrorState } from "@/components/common/ErrorState";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { LoadingSpinner } from "@/components/common/LoadingSpinner";
import { Alert } from "@/components/ui/Alert";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import type { RecipeDetail } from "@/types/recipe";

const sampleDetail: RecipeDetail = {
  id: "rec-preview-001",
  title: "Phở Bò Tái Nạm Hà Nội",
  slug: "pho-bo-tai-nam-ha-noi",
  description: "Món phở bò gia truyền với nước dùng hầm xương ngọt thanh, thơm mùi hồi quế và gừng nướng.",
  content: "Bí quyết của món phở ngon nằm ở khâu ninh xương ống bò ít nhất 8 tiếng, vớt bọt liên tục để nước dùng trong vắt.",
  prepTimeMinutes: 30,
  cookTimeMinutes: 180,
  servings: 4,
  difficulty: "Medium",
  status: "Published",
  publishedAt: "2026-10-21T08:00:00Z",
  createdAt: "2026-10-20T08:00:00Z",
  updatedAt: null,
  category: {
    id: "cat-001",
    name: "Món Việt",
    slug: "mon-viet",
  },
  author: {
    id: "user-001",
    displayName: "Bếp Trưởng Hà Nội",
    avatarUrl: null,
  },
  nutrition: {
    calories: 520,
    protein: 35,
    carbohydrates: 65,
    fat: 12,
    fiber: 3,
    sodium: 980,
    source: "Tính toán dinh dưỡng chuẩn",
  },
  images: [
    {
      id: "img-001",
      originalUrl: "/sample-images/food-01.svg",
      mediumUrl: null,
      thumbnailUrl: null,
      altText: "Tô phở bò nóng hổi",
      isPrimary: true,
      orderIndex: 0,
      url: "/sample-images/food-01.svg",
    },
  ],
  ingredients: [
    { id: "ing-1", name: "Bánh phở tươi", quantity: 500, unit: "g", notes: "chần qua nước sôi", orderIndex: 0 },
    { id: "ing-2", name: "Thịt bò tái & nạm", quantity: 300, unit: "g", notes: "thái mỏng", orderIndex: 1 },
    { id: "ing-3", name: "Xương ống bò", quantity: 1, unit: "kg", notes: "hầm lấy nước dùng", orderIndex: 2 },
  ],
  steps: [
    { id: "step-1", stepNumber: 1, title: "Sơ chế xương bò", description: "Ngâm rửa sạch xương bò với muối và gừng, chần qua nước sôi.", timerMinutes: 15, imageUrl: null },
    { id: "step-2", stepNumber: 2, title: "Ninh nước dùng", description: "Cho gừng, hành nướng, hoa hồi, thảo quả vào nồi xương ninh nhỏ lửa.", timerMinutes: 180, imageUrl: null },
  ],
};

export default function ComponentsPreviewPage() {
  const [activeTab, setActiveTab] = useState<"recipe" | "step" | "image" | "ui">("recipe");
  const [showConfirm, setShowConfirm] = useState(false);
  const [confirmResult, setConfirmResult] = useState<string | null>(null);

  return (
    <div className="container-page py-10 space-y-10">
      <div className="border-b border-gray-200 pb-5">
        <h1 className="text-3xl font-extrabold text-emerald-900">Task 6 Shared Components Preview</h1>
        <p className="text-sm text-gray-500 mt-1">
          Môi trường kiểm thử giao diện tĩnh nghiệm thu Task 6 của 4 thành viên (Quốc, Nhung, Phước, Mạnh).
        </p>

        {/* Tab switcher */}
        <div className="flex gap-2 mt-6">
          <Button
            variant={activeTab === "recipe" ? "default" : "outline"}
            onClick={() => setActiveTab("recipe")}
            className="text-xs"
          >
            Recipe Detail & Form (Phước)
          </Button>
          <Button
            variant={activeTab === "step" ? "default" : "outline"}
            onClick={() => setActiveTab("step")}
            className="text-xs"
          >
            StepEditor & StepItem (Mạnh)
          </Button>
          <Button
            variant={activeTab === "image" ? "default" : "outline"}
            onClick={() => setActiveTab("image")}
            className="text-xs"
          >
            ImageManager & Preview (Mạnh)
          </Button>
          <Button
            variant={activeTab === "ui" ? "default" : "outline"}
            onClick={() => setActiveTab("ui")}
            className="text-xs"
          >
            Shared UI & Dialogs (Nhung)
          </Button>
        </div>
      </div>

      {activeTab === "recipe" && (
        <section className="space-y-10">
          <div>
            <h2 className="text-lg font-bold text-gray-800 mb-4">1. Giao diện tĩnh Recipe Detail (Phạm Nguyễn Ngọc Phước)</h2>
            <div className="bg-white p-6 rounded-2xl border border-gray-100 shadow-xs">
              <RecipeDetailView recipe={sampleDetail} />
            </div>
          </div>

          <div>
            <h2 className="text-lg font-bold text-gray-800 mb-4">2. Bố cục Recipe Form Layout tĩnh (Phạm Nguyễn Ngọc Phước)</h2>
            <div className="bg-white p-6 rounded-2xl border border-gray-100 shadow-xs">
              <RecipeFormLayout status="Draft">
                <div className="p-4 border border-dashed border-emerald-300 rounded-xl text-center text-sm text-emerald-700 bg-emerald-50/50">
                  Container chứa Basic Info, Ingredient, Nutrition, StepEditor và ImageManager sẵn sàng cho Task 8.
                </div>
              </RecipeFormLayout>
            </div>
          </div>
        </section>
      )}

      {activeTab === "step" && (
        <section className="space-y-6">
          <h2 className="text-lg font-bold text-gray-800">Giao diện StepEditor (Võ Hùng Mạnh)</h2>
          <div className="bg-white p-6 rounded-2xl border border-gray-100 shadow-xs">
            <StepEditor initialSteps={sampleDetail.steps} />
          </div>
        </section>
      )}

      {activeTab === "image" && (
        <section className="space-y-6">
          <h2 className="text-lg font-bold text-gray-800">Giao diện ImageManager (Võ Hùng Mạnh)</h2>
          <div className="bg-white p-6 rounded-2xl border border-gray-100 shadow-xs">
            <ImageManager initialImages={sampleDetail.images} />
          </div>
        </section>
      )}

      {activeTab === "ui" && (
        <section className="space-y-8">
          <h2 className="text-lg font-bold text-gray-800">Thư viện UI và Shared Components (Lê Thị Ánh Nhung)</h2>

          {/* Badges & Alerts */}
          <div className="space-y-4">
            <h3 className="text-sm font-semibold text-gray-700">Badges & Alerts</h3>
            <div className="flex flex-wrap gap-2">
              <Badge variant="default">Default Badge</Badge>
              <Badge variant="secondary">Secondary Badge</Badge>
              <Badge variant="outline">Outline Badge</Badge>
              <Badge variant="destructive">Destructive Badge</Badge>
            </div>
            <div className="space-y-2 max-w-xl">
              <Alert variant="default" title="Thông báo hệ thống">
                Thư viện UI shadcn/ui đã được tích hợp thành công trên Blog App.
              </Alert>
              <Alert variant="destructive" title="Cảnh báo kiểm thử">
                Đây là alert mẫu kiểm tra giao diện lỗi tĩnh.
              </Alert>
            </div>
          </div>

          {/* ConfirmDialog */}
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-gray-700">ConfirmDialog</h3>
            <Button onClick={() => setShowConfirm(true)} variant="default" className="text-xs">
              Mở Confirm Dialog Thử Nghiệm
            </Button>
            {confirmResult && <p className="text-xs text-gray-600">Kết quả: {confirmResult}</p>}

            <ConfirmDialog
              isOpen={showConfirm}
              title="Xác nhận xóa công thức?"
              message="Hành động này sẽ chuyển công thức vào thùng rác. Bạn có chắc chắn muốn tiếp tục?"
              confirmText="Đồng ý xóa"
              cancelText="Hủy bỏ"
              variant="destructive"
              onConfirm={() => {
                setConfirmResult("Đã xác nhận hành động!");
                setShowConfirm(false);
              }}
              onCancel={() => setShowConfirm(false)}
            />
          </div>

          {/* Loading & Empty States */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="p-4 bg-white rounded-xl border border-gray-100">
              <span className="text-xs font-bold text-gray-500 uppercase block mb-3">Loading Spinner & Skeleton</span>
              <div className="flex items-center gap-4 mb-4">
                <LoadingSpinner size="sm" />
                <LoadingSpinner size="md" />
                <LoadingSpinner size="lg" />
              </div>
              <LoadingSkeleton variant="card" count={1} />
            </div>

            <div className="p-4 bg-white rounded-xl border border-gray-100">
              <span className="text-xs font-bold text-gray-500 uppercase block mb-3">Empty & Error States</span>
              <EmptyState
                title="Chưa có dữ liệu"
                description="Danh sách dữ liệu hiện đang trống."
              />
              <div className="mt-4">
                <ErrorState
                  title="Lỗi tải dữ liệu"
                  message="Không thể kết nối đến máy chủ."
                  onRetry={() => alert("Thử lại thành công!")}
                />
              </div>
            </div>
          </div>
        </section>
      )}
    </div>
  );
}
