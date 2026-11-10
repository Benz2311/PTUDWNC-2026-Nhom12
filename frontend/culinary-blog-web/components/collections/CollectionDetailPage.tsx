"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  FolderHeart,
  ArrowLeft,
  Lock,
  Globe,
  Edit2,
  Trash2,
  XCircle,
  Clock,
} from "lucide-react";
import type { RecipeCollectionDetailDto, UpdateCollectionPayload } from "@/types/collection";
import {
  getCollectionById,
  updateCollection,
  deleteCollection,
  removeRecipeFromCollection,
} from "@/lib/api/collections";
import { CollectionModal } from "./CollectionModal";
import { ConfirmDialog } from "@/components/common/ConfirmDialog";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { EmptyState } from "@/components/common/EmptyState";
import { ErrorState } from "@/components/common/ErrorState";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Card } from "@/components/ui/Card";
import DifficultyBadge from "@/components/recipe/DifficultyBadge";
import { resolveImageUrl } from "@/lib/image-url";

interface CollectionDetailPageProps {
  id: string;
}

export function CollectionDetailPage({ id }: CollectionDetailPageProps) {
  const router = useRouter();
  const [collection, setCollection] = useState<RecipeCollectionDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Edit Modal State
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);

  // Delete Collection Confirm
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  // Remove Recipe Confirm
  const [removingRecipeId, setRemovingRecipeId] = useState<string | null>(null);

  const fetchDetail = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getCollectionById(id);
      setCollection(data);
    } catch (err: any) {
      console.error("Failed to load collection detail:", err);
      setError("Không thể tìm thấy bộ sưu tập hoặc bạn không có quyền truy cập.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDetail();
  }, [id]);

  const handleUpdate = async (payload: UpdateCollectionPayload) => {
    setActionLoading(true);
    try {
      await updateCollection(id, payload);
      await fetchDetail();
      setIsEditOpen(false);
    } finally {
      setActionLoading(false);
    }
  };

  const handleDeleteCollection = async () => {
    setActionLoading(true);
    try {
      await deleteCollection(id);
      router.push("/collections");
    } finally {
      setActionLoading(false);
    }
  };

  const handleRemoveRecipe = async () => {
    if (!removingRecipeId) return;
    setActionLoading(true);
    try {
      await removeRecipeFromCollection(id, removingRecipeId);
      setCollection((prev) =>
        prev
          ? {
              ...prev,
              recipes: prev.recipes.filter((r) => r.recipeId !== removingRecipeId),
            }
          : prev
      );
      setRemovingRecipeId(null);
    } catch (err) {
      console.error("Failed to remove recipe from collection:", err);
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-48 bg-gray-200 animate-pulse rounded-xl" />
        <div className="h-32 bg-gray-100 animate-pulse rounded-3xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {Array.from({ length: 3 }).map((_, i) => (
            <LoadingSkeleton key={i} variant="card" />
          ))}
        </div>
      </div>
    );
  }

  if (error || !collection) {
    return (
      <ErrorState
        title="Không tìm thấy bộ sưu tập"
        message={error || "Bộ sưu tập này không tồn tại hoặc đã bị xóa."}
        onRetry={fetchDetail}
      />
    );
  }

  return (
    <div className="space-y-8" data-testid="collection-detail-page">
      {/* Back button */}
      <div>
        <Link
          href="/collections"
          className="inline-flex items-center gap-2 text-xs font-bold text-gray-500 hover:text-emerald-700 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          <span>Quay lại tất cả bộ sưu tập</span>
        </Link>
      </div>

      {/* Collection Header Banner */}
      <div className="rounded-3xl bg-linear-to-br from-emerald-50 via-white to-teal-50/40 border border-emerald-100 p-6 sm:p-8 shadow-xs">
        <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-2">
              <Badge
                variant="secondary"
                className={`text-[11px] font-bold px-2.5 py-1 rounded-full ${
                  collection.isPrivate
                    ? "bg-amber-100 text-amber-900 border-amber-200"
                    : "bg-emerald-100 text-emerald-900 border-emerald-200"
                }`}
              >
                {collection.isPrivate ? (
                  <span className="flex items-center gap-1">
                    <Lock className="h-3 w-3" /> Bộ sưu tập riêng tư
                  </span>
                ) : (
                  <span className="flex items-center gap-1">
                    <Globe className="h-3 w-3" /> Bộ sưu tập công khai
                  </span>
                )}
              </Badge>
              <span className="text-xs font-semibold text-gray-500">
                {collection.recipes.length} công thức
              </span>
            </div>

            <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
              {collection.name}
            </h1>

            {collection.description && (
              <p className="mt-2 text-sm text-gray-600 max-w-2xl">
                {collection.description}
              </p>
            )}

            <div className="mt-4 flex items-center gap-4 text-xs text-gray-400">
              <span>Chủ sở hữu: <strong>{collection.userName}</strong></span>
              <span>•</span>
              <span>Cập nhật: {new Date(collection.updatedAt || collection.createdAt).toLocaleDateString("vi-VN")}</span>
            </div>
          </div>

          {/* Action buttons */}
          <div className="flex items-center gap-2 self-start">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsEditOpen(true)}
              className="rounded-xl text-xs border-gray-200 font-bold"
            >
              <Edit2 className="h-3.5 w-3.5 mr-1.5" />
              Đổi tên & Mô tả
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsDeleteOpen(true)}
              className="rounded-xl text-xs border-rose-200 text-rose-600 hover:bg-rose-50 font-bold"
            >
              <Trash2 className="h-3.5 w-3.5 mr-1.5" />
              Xóa
            </Button>
          </div>
        </div>
      </div>

      {/* Recipes in Collection */}
      <div>
        <h2 className="text-lg font-bold text-gray-900 mb-4 flex items-center gap-2">
          <FolderHeart className="h-5 w-5 text-emerald-700" />
          <span>Danh sách công thức ({collection.recipes.length})</span>
        </h2>

        {collection.recipes.length === 0 ? (
          <EmptyState
            title="Bộ sưu tập này chưa có công thức nào"
            description="Hãy khám phá các món ăn trên trang chủ hoặc trang tìm kiếm và nhấn 'Lưu vào bộ sưu tập' để thêm nhé!"
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {collection.recipes.map((item) => {
              const totalTime = (item.prepTimeMinutes || 0) + (item.cookTimeMinutes || 0);
              const imgUrl = resolveImageUrl(item.coverImageUrl) || "/sample-images/food-01.svg";

              return (
                <Card
                  key={item.recipeId}
                  className="group relative flex flex-col justify-between bg-white rounded-2xl border border-gray-100 overflow-hidden shadow-xs hover:shadow-xl hover:border-emerald-200 transition-all duration-300"
                >
                  <div className="relative aspect-video w-full overflow-hidden bg-gray-100">
                    <img
                      src={imgUrl}
                      alt={item.title}
                      className="h-full w-full object-cover group-hover:scale-105 transition-transform duration-500"
                      loading="lazy"
                    />
                    <div className="absolute top-3 left-3">
                      <Badge variant="secondary" className="bg-white/90 backdrop-blur-xs text-gray-800 text-[11px] font-bold">
                        {item.categoryName}
                      </Badge>
                    </div>

                    {/* Quick remove from collection button */}
                    <button
                      type="button"
                      onClick={() => setRemovingRecipeId(item.recipeId)}
                      className="absolute top-3 right-3 p-1.5 rounded-full bg-white/90 text-gray-500 hover:text-rose-600 hover:bg-white shadow-md transition-colors cursor-pointer"
                      title="Xóa công thức khỏi bộ sưu tập này"
                    >
                      <XCircle className="h-4 w-4" />
                    </button>
                  </div>

                  <div className="p-5 flex flex-col flex-1 justify-between">
                    <div>
                      <h3 className="font-bold text-gray-900 text-base line-clamp-2 group-hover:text-emerald-700 transition-colors">
                        <Link href={`/recipes/${item.slug}`}>
                          {item.title}
                        </Link>
                      </h3>
                    </div>

                    <div className="pt-4 mt-4 border-t border-gray-100 flex items-center justify-between text-xs text-gray-600">
                      <div className="flex items-center gap-3">
                        {totalTime > 0 && (
                          <span className="flex items-center gap-1">
                            <Clock className="h-3.5 w-3.5 text-gray-400" />
                            {totalTime}p
                          </span>
                        )}
                        <DifficultyBadge difficulty={item.difficulty} />
                      </div>

                      <Link
                        href={`/recipes/${item.slug}`}
                        className="font-bold text-emerald-700 hover:text-emerald-800 text-xs"
                      >
                        Nấu ngay
                      </Link>
                    </div>
                  </div>
                </Card>
              );
            })}
          </div>
        )}
      </div>

      {/* Edit Modal */}
      <CollectionModal
        isOpen={isEditOpen}
        onClose={() => setIsEditOpen(false)}
        onSubmit={handleUpdate}
        initialData={{
          id: collection.id,
          name: collection.name,
          description: collection.description,
          isPrivate: collection.isPrivate,
          userId: collection.userId,
          userName: collection.userName,
          recipeCount: collection.recipes.length,
          createdAt: collection.createdAt,
          updatedAt: collection.updatedAt,
          previewImageUrls: [],
        }}
        loading={actionLoading}
      />

      {/* Delete Collection Confirm */}
      <ConfirmDialog
        isOpen={isDeleteOpen}
        title="Xóa bộ sưu tập"
        message={`Bạn có chắc chắn muốn xóa bộ sưu tập "${collection.name}"?`}
        confirmText="Xóa vĩnh viễn"
        cancelText="Hủy"
        variant="destructive"
        onConfirm={handleDeleteCollection}
        onCancel={() => setIsDeleteOpen(false)}
        isLoading={actionLoading}
      />

      {/* Remove Recipe Confirm */}
      <ConfirmDialog
        isOpen={removingRecipeId !== null}
        title="Bỏ công thức khỏi bộ sưu tập"
        message="Bạn có chắc chắn muốn loại bỏ công thức này khỏi bộ sưu tập hiện tại?"
        confirmText="Loại bỏ"
        cancelText="Giữ lại"
        variant="destructive"
        onConfirm={handleRemoveRecipe}
        onCancel={() => setRemovingRecipeId(null)}
        isLoading={actionLoading}
      />
    </div>
  );
}

export default CollectionDetailPage;
