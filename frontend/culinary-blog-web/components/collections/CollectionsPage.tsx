"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { FolderHeart, Plus, Lock, Globe, Trash2, Edit2, Utensils, ArrowRight } from "lucide-react";
import type { RecipeCollectionDto, CreateCollectionPayload } from "@/types/collection";
import {
  getUserCollections,
  createCollection,
  updateCollection,
  deleteCollection,
} from "@/lib/api/collections";
import { CollectionModal } from "./CollectionModal";
import { ConfirmDialog } from "@/components/common/ConfirmDialog";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { EmptyState } from "@/components/common/EmptyState";
import { ErrorState } from "@/components/common/ErrorState";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { resolveImageUrl } from "@/lib/image-url";

export function CollectionsPage() {
  const [collections, setCollections] = useState<RecipeCollectionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingCollection, setEditingCollection] = useState<RecipeCollectionDto | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  // Delete Confirm State
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);

  const fetchCollections = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getUserCollections();
      setCollections(data);
    } catch (err: any) {
      console.error("Failed to load collections:", err);
      setError("Không thể tải danh sách bộ sưu tập. Vui lòng đăng nhập và thử lại.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCollections();
  }, []);

  const handleCreateOrUpdate = async (payload: CreateCollectionPayload) => {
    setActionLoading(true);
    try {
      if (editingCollection) {
        const updated = await updateCollection(editingCollection.id, payload);
        setCollections((prev) =>
          prev.map((c) => (c.id === updated.id ? updated : c))
        );
      } else {
        const created = await createCollection(payload);
        setCollections((prev) => [created, ...prev]);
      }
      setIsModalOpen(false);
      setEditingCollection(null);
    } finally {
      setActionLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!deletingId) return;
    setActionLoading(true);
    try {
      await deleteCollection(deletingId);
      setCollections((prev) => prev.filter((c) => c.id !== deletingId));
      setIsConfirmOpen(false);
      setDeletingId(null);
    } catch (err) {
      console.error("Error deleting collection:", err);
    } finally {
      setActionLoading(false);
    }
  };

  return (
    <div className="space-y-8" data-testid="collections-page">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-gray-100">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-100 text-emerald-800 text-xs font-bold mb-2">
            <FolderHeart className="h-3.5 w-3.5 text-emerald-600" />
            <span>Bộ sưu tập cá nhân</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
            Bộ sưu tập công thức
          </h1>
          <p className="text-xs sm:text-sm text-gray-600 mt-1">
            Lưu trữ, phân loại và sắp xếp các món ăn yêu thích theo ý muốn của bạn
          </p>
        </div>

        <Button
          onClick={() => {
            setEditingCollection(null);
            setIsModalOpen(true);
          }}
          className="rounded-2xl bg-emerald-700 hover:bg-emerald-800 text-white font-bold text-xs sm:text-sm shadow-md cursor-pointer self-start sm:self-auto"
        >
          <Plus className="h-4 w-4 mr-2" />
          Tạo bộ sưu tập mới
        </Button>
      </div>

      {/* Content */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {Array.from({ length: 6 }).map((_, i) => (
            <LoadingSkeleton key={i} variant="card" />
          ))}
        </div>
      ) : error ? (
        <ErrorState
          title="Không thể tải bộ sưu tập"
          message={error}
          onRetry={fetchCollections}
        />
      ) : collections.length === 0 ? (
        <EmptyState
          title="Bạn chưa có bộ sưu tập nào"
          description="Hãy tạo bộ sưu tập đầu tiên để gom các món ăn thơm ngon yêu thích nhé!"
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {collections.map((item) => (
            <Card
              key={item.id}
              className="group relative flex flex-col justify-between bg-white rounded-3xl border border-gray-100 p-6 shadow-xs hover:shadow-xl hover:border-emerald-200 transition-all duration-300"
            >
              <div>
                {/* Header of Card */}
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1.5">
                      <Badge
                        variant="secondary"
                        className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${
                          item.isPrivate
                            ? "bg-amber-50 text-amber-800 border-amber-200"
                            : "bg-emerald-50 text-emerald-800 border-emerald-200"
                        }`}
                      >
                        {item.isPrivate ? (
                          <span className="flex items-center gap-1">
                            <Lock className="h-3 w-3" /> Riêng tư
                          </span>
                        ) : (
                          <span className="flex items-center gap-1">
                            <Globe className="h-3 w-3" /> Công khai
                          </span>
                        )}
                      </Badge>
                      <span className="text-xs font-semibold text-gray-500">
                        {item.recipeCount} công thức
                      </span>
                    </div>

                    <h3 className="font-bold text-gray-900 text-lg line-clamp-1 group-hover:text-emerald-700 transition-colors">
                      <Link href={`/collections/${item.id}`}>
                        {item.name}
                      </Link>
                    </h3>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => {
                        setEditingCollection(item);
                        setIsModalOpen(true);
                      }}
                      className="p-1.5 rounded-lg text-gray-400 hover:text-emerald-700 hover:bg-emerald-50 transition-colors cursor-pointer"
                      title="Chỉnh sửa bộ sưu tập"
                    >
                      <Edit2 className="h-4 w-4" />
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        setDeletingId(item.id);
                        setIsConfirmOpen(true);
                      }}
                      className="p-1.5 rounded-lg text-gray-400 hover:text-rose-600 hover:bg-rose-50 transition-colors cursor-pointer"
                      title="Xóa bộ sưu tập"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>

                {item.description && (
                  <p className="text-xs text-gray-500 mt-2 line-clamp-2">
                    {item.description}
                  </p>
                )}

                {/* Preview Images Thumbnail Collage */}
                <div className="mt-5">
                  {item.previewImageUrls && item.previewImageUrls.length > 0 ? (
                    <div className="grid grid-cols-4 gap-2 h-20 rounded-2xl overflow-hidden bg-gray-50 p-1 border border-gray-100">
                      {item.previewImageUrls.slice(0, 4).map((url, idx) => (
                        <div key={idx} className="relative h-full w-full rounded-xl overflow-hidden bg-gray-200">
                          <img
                            src={resolveImageUrl(url) || "/sample-images/food-01.svg"}
                            alt=""
                            className="h-full w-full object-cover"
                          />
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="h-20 rounded-2xl bg-gray-50 border border-dashed border-gray-200 flex items-center justify-center text-gray-400 text-xs">
                      <Utensils className="h-4 w-4 mr-2" />
                      Chưa có công thức nào
                    </div>
                  )}
                </div>
              </div>

              {/* View details footer link */}
              <div className="pt-4 mt-5 border-t border-gray-100 flex items-center justify-between">
                <span className="text-[11px] text-gray-400">
                  Tạo ngày {new Date(item.createdAt).toLocaleDateString("vi-VN")}
                </span>
                <Link
                  href={`/collections/${item.id}`}
                  className="inline-flex items-center text-xs font-bold text-emerald-700 hover:text-emerald-800 gap-1"
                >
                  <span>Xem chi tiết</span>
                  <ArrowRight className="h-3.5 w-3.5" />
                </Link>
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Create / Edit Modal */}
      <CollectionModal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEditingCollection(null);
        }}
        onSubmit={handleCreateOrUpdate}
        initialData={editingCollection}
        loading={actionLoading}
      />

      {/* Delete Confirm Dialog */}
      <ConfirmDialog
        isOpen={isConfirmOpen}
        title="Xóa bộ sưu tập"
        message="Bạn có chắc chắn muốn xóa bộ sưu tập này? Các công thức bên trong sẽ không bị xóa khỏi hệ thống."
        confirmText="Xóa bộ sưu tập"
        cancelText="Hủy"
        variant="destructive"
        onConfirm={handleDelete}
        onCancel={() => {
          setIsConfirmOpen(false);
          setDeletingId(null);
        }}
        isLoading={actionLoading}
      />
    </div>
  );
}

export default CollectionsPage;
