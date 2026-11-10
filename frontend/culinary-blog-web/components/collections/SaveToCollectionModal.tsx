"use client";

import React, { useState, useEffect } from "react";
import { FolderHeart, Plus, Check, X, Loader2 } from "lucide-react";
import type { RecipeCollectionDto } from "@/types/collection";
import {
  getUserCollections,
  createCollection,
  addRecipeToCollection,
  removeRecipeFromCollection,
  getRecipeCollectionsForUser,
} from "@/lib/api/collections";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";

interface SaveToCollectionModalProps {
  recipeId: string;
  recipeTitle: string;
  isOpen: boolean;
  onClose: () => void;
}

export function SaveToCollectionModal({
  recipeId,
  recipeTitle,
  isOpen,
  onClose,
}: SaveToCollectionModalProps) {
  const [collections, setCollections] = useState<RecipeCollectionDto[]>([]);
  const [selectedCollectionIds, setSelectedCollectionIds] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [newCollectionName, setNewCollectionName] = useState("");
  const [creating, setCreating] = useState(false);
  const [actionInProgress, setActionInProgress] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen || !recipeId) return;

    let mounted = true;
    setLoading(true);

    Promise.all([
      getUserCollections(),
      getRecipeCollectionsForUser(recipeId),
    ])
      .then(([cols, includedIds]) => {
        if (!mounted) return;
        setCollections(cols);
        setSelectedCollectionIds(new Set(includedIds));
      })
      .catch((err) => console.error("Error loading collections modal:", err))
      .finally(() => {
        if (mounted) setLoading(false);
      });

    return () => {
      mounted = false;
    };
  }, [isOpen, recipeId]);

  if (!isOpen) return null;

  const handleToggle = async (collectionId: string) => {
    const isCurrentlySaved = selectedCollectionIds.has(collectionId);
    setActionInProgress(collectionId);

    try {
      if (isCurrentlySaved) {
        await removeRecipeFromCollection(collectionId, recipeId);
        setSelectedCollectionIds((prev) => {
          const next = new Set(prev);
          next.delete(collectionId);
          return next;
        });
      } else {
        await addRecipeToCollection(collectionId, recipeId);
        setSelectedCollectionIds((prev) => {
          const next = new Set(prev);
          next.add(collectionId);
          return next;
        });
      }
    } catch (err) {
      console.error("Failed to toggle recipe collection:", err);
    } finally {
      setActionInProgress(null);
    }
  };

  const handleCreateAndAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newCollectionName.trim()) return;

    setCreating(true);
    try {
      const created = await createCollection({
        name: newCollectionName.trim(),
        isPrivate: false,
      });
      await addRecipeToCollection(created.id, recipeId);
      setCollections((prev) => [created, ...prev]);
      setSelectedCollectionIds((prev) => new Set(prev).add(created.id));
      setNewCollectionName("");
    } catch (err) {
      console.error("Failed to create collection and add recipe:", err);
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-3xl max-w-sm w-full p-6 shadow-2xl border border-gray-100 flex flex-col max-h-[85vh]">
        {/* Header */}
        <div className="flex items-center justify-between pb-3 border-b border-gray-100">
          <div className="flex items-center gap-2 text-emerald-800">
            <FolderHeart className="h-5 w-5" />
            <h3 className="text-base font-bold text-gray-900">
              Lưu vào bộ sưu tập
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

        <p className="text-xs text-gray-500 mt-2 line-clamp-1">
          Chọn bộ sưu tập để lưu <strong>{recipeTitle}</strong>:
        </p>

        {/* Collections List */}
        <div className="flex-1 overflow-y-auto py-3 space-y-2 pr-1 my-2">
          {loading ? (
            <div className="space-y-2 py-4">
              <div className="h-10 bg-gray-100 rounded-xl animate-pulse" />
              <div className="h-10 bg-gray-100 rounded-xl animate-pulse" />
            </div>
          ) : collections.length === 0 ? (
            <p className="text-xs text-gray-400 text-center py-4">
              Bạn chưa có bộ sưu tập nào. Hãy tạo một bộ sưu tập ở bên dưới!
            </p>
          ) : (
            collections.map((col) => {
              const isSelected = selectedCollectionIds.has(col.id);
              const isBusy = actionInProgress === col.id;

              return (
                <button
                  key={col.id}
                  type="button"
                  onClick={() => handleToggle(col.id)}
                  disabled={isBusy}
                  className={`w-full flex items-center justify-between p-3 rounded-2xl border transition-all cursor-pointer text-left ${
                    isSelected
                      ? "bg-emerald-50/80 border-emerald-300 text-emerald-950 font-bold"
                      : "bg-gray-50/60 border-gray-100 hover:bg-gray-100 text-gray-700 font-medium"
                  }`}
                >
                  <span className="text-xs line-clamp-1">{col.name}</span>
                  <div
                    className={`h-5 w-5 rounded-md flex items-center justify-center transition-colors ${
                      isSelected
                        ? "bg-emerald-700 text-white"
                        : "border border-gray-300 bg-white"
                    }`}
                  >
                    {isBusy ? (
                      <Loader2 className="h-3 w-3 animate-spin text-emerald-700" />
                    ) : isSelected ? (
                      <Check className="h-3.5 w-3.5" />
                    ) : null}
                  </div>
                </button>
              );
            })
          )}
        </div>

        {/* Quick create inline form */}
        <form onSubmit={handleCreateAndAdd} className="pt-3 border-t border-gray-100 flex items-center gap-2">
          <Input
            type="text"
            placeholder="Tạo bộ sưu tập mới..."
            value={newCollectionName}
            onChange={(e) => setNewCollectionName(e.target.value)}
            className="rounded-xl text-xs flex-1"
            disabled={creating}
          />
          <Button
            type="submit"
            size="sm"
            disabled={creating || !newCollectionName.trim()}
            className="rounded-xl text-xs bg-emerald-700 hover:bg-emerald-800 text-white font-bold"
          >
            {creating ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Plus className="h-3.5 w-3.5" />}
          </Button>
        </form>

        <div className="pt-3 flex justify-end">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onClose}
            className="rounded-xl text-xs"
          >
            Hoàn tất
          </Button>
        </div>
      </div>
    </div>
  );
}

export default SaveToCollectionModal;
