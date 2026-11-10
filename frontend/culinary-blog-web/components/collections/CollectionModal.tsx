"use client";

import React, { useState, useEffect } from "react";
import { X, FolderPlus, Edit3, Lock, Globe } from "lucide-react";
import type { RecipeCollectionDto, CreateCollectionPayload } from "@/types/collection";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";

interface CollectionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: CreateCollectionPayload) => Promise<void>;
  initialData?: RecipeCollectionDto | null;
  loading?: boolean;
}

export function CollectionModal({
  isOpen,
  onClose,
  onSubmit,
  initialData,
  loading = false,
}: CollectionModalProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (initialData) {
      setName(initialData.name);
      setDescription(initialData.description || "");
      setIsPrivate(initialData.isPrivate);
    } else {
      setName("");
      setDescription("");
      setIsPrivate(false);
    }
    setError(null);
  }, [initialData, isOpen]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      setError("Vui lòng nhập tên bộ sưu tập.");
      return;
    }

    try {
      await onSubmit({
        name: name.trim(),
        description: description.trim() || undefined,
        isPrivate,
      });
      onClose();
    } catch (err: any) {
      setError(err?.message || "Có lỗi xảy ra khi lưu bộ sưu tập.");
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-3xl max-w-md w-full p-6 shadow-2xl border border-gray-100">
        <div className="flex items-center justify-between pb-4 border-b border-gray-100">
          <div className="flex items-center gap-2 text-emerald-800">
            {initialData ? (
              <Edit3 className="h-5 w-5" />
            ) : (
              <FolderPlus className="h-5 w-5" />
            )}
            <h3 className="text-lg font-bold text-gray-900">
              {initialData ? "Chỉnh sửa bộ sưu tập" : "Tạo bộ sưu tập mới"}
            </h3>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-full text-gray-400 hover:text-gray-600 hover:bg-gray-100 cursor-pointer"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-5 space-y-4">
          {error && (
            <div className="p-3 text-xs text-rose-700 bg-rose-50 border border-rose-200 rounded-xl">
              {error}
            </div>
          )}

          <div>
            <label className="block text-xs font-bold text-gray-700 mb-1">
              Tên bộ sưu tập <span className="text-rose-500">*</span>
            </label>
            <Input
              type="text"
              placeholder="VD: Món ăn sáng yêu thích, Món chay thanh đạm..."
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="rounded-xl text-sm"
              maxLength={100}
              required
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-700 mb-1">
              Mô tả ngắn
            </label>
            <Textarea
              placeholder="Ghi chú thêm về bộ sưu tập này..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="rounded-xl text-sm resize-none"
              rows={3}
              maxLength={500}
            />
          </div>

          <div className="pt-2">
            <label className="flex items-center gap-3 p-3 rounded-2xl border border-gray-200 bg-gray-50/50 cursor-pointer hover:bg-gray-50">
              <input
                type="checkbox"
                checked={isPrivate}
                onChange={(e) => setIsPrivate(e.target.checked)}
                className="h-4 w-4 text-emerald-600 rounded border-gray-300 focus:ring-emerald-500"
              />
              <div className="flex-1">
                <div className="flex items-center gap-1.5 text-xs font-bold text-gray-800">
                  {isPrivate ? (
                    <>
                      <Lock className="h-3.5 w-3.5 text-amber-600" />
                      <span>Riêng tư (Chỉ bạn xem được)</span>
                    </>
                  ) : (
                    <>
                      <Globe className="h-3.5 w-3.5 text-emerald-600" />
                      <span>Công khai (Mọi người có thể xem)</span>
                    </>
                  )}
                </div>
                <p className="text-[11px] text-gray-500 mt-0.5">
                  {isPrivate
                    ? "Bộ sưu tập này sẽ không hiển thị trên hồ sơ công khai của bạn."
                    : "Mọi người có thể khám phá công thức từ bộ sưu tập này của bạn."}
                </p>
              </div>
            </label>
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-gray-100">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={loading}
              className="rounded-xl text-xs"
            >
              Hủy
            </Button>
            <Button
              type="submit"
              disabled={loading || !name.trim()}
              className="rounded-xl text-xs bg-emerald-700 hover:bg-emerald-800 text-white font-bold"
            >
              {loading ? "Đang lưu..." : initialData ? "Cập nhật" : "Tạo mới"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default CollectionModal;

