"use client";

import { useEffect, useState } from "react";
import ProtectedRoute from "@/components/auth/ProtectedRoute";
import { ApiError } from "@/lib/api/client";
import {
  getTrashRecipes,
  purgeRecipe,
  restoreRecipe,
} from "@/lib/api/recipes";
import type { RecipeTrashItem } from "@/types/recipe";

export default function AdminRecipeTrashPage() {
  const [items, setItems] = useState<RecipeTrashItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function loadTrash() {
    setLoading(true);
    setError(null);
    try {
      const result = await getTrashRecipes();
      setItems(result.items);
    } catch (requestError) {
      setError(
        requestError instanceof ApiError
          ? requestError.title || requestError.message
          : "Không thể tải thùng rác công thức."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadTrash().catch(() => undefined);
  }, []);

  async function handleRestore(id: string) {
    await restoreRecipe(id);
    await loadTrash();
  }

  async function handlePurge(id: string) {
    if (!window.confirm("Xóa vĩnh viễn công thức này?")) return;
    await purgeRecipe(id);
    await loadTrash();
  }

  return (
    <ProtectedRoute roles={["Admin"]}>
      <main className="min-h-screen bg-gray-50 p-8">
        <div className="mx-auto max-w-5xl">
          <h1 className="text-3xl font-bold text-gray-900">Thùng rác công thức</h1>
          <p className="mt-1 text-gray-500">Khôi phục hoặc xóa vĩnh viễn công thức đã xóa.</p>
          {error && (
            <div className="mt-6 rounded-lg border border-red-200 bg-red-50 p-4 text-red-700">
              {error}
              <button className="ml-4 underline" onClick={() => { loadTrash().catch(() => undefined); }}>Thử lại</button>
            </div>
          )}
          {loading ? (
            <p className="mt-8 text-gray-500">Đang tải...</p>
          ) : items.length === 0 ? (
            <p className="mt-8 rounded-xl bg-white p-8 text-center text-gray-500">Thùng rác đang trống.</p>
          ) : (
            <div className="mt-6 overflow-hidden rounded-xl border bg-white">
              {items.map((item) => (
                <div key={item.id} className="flex items-center justify-between border-b p-4 last:border-b-0">
                  <div>
                    <p className="font-semibold text-gray-900">{item.title}</p>
                    <p className="text-sm text-gray-500">
                      Đã xóa: {item.deletedAt ? new Date(item.deletedAt).toLocaleDateString("vi-VN") : "Không rõ"}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <button className="rounded-lg bg-teal-600 px-3 py-2 text-sm font-semibold text-white" onClick={() => { handleRestore(item.id).catch(() => undefined); }}>
                      Khôi phục
                    </button>
                    <button className="rounded-lg border border-red-300 px-3 py-2 text-sm font-semibold text-red-700" onClick={() => { handlePurge(item.id).catch(() => undefined); }}>
                      Xóa vĩnh viễn
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </main>
    </ProtectedRoute>
  );
}
