"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import {
  getCategories,
  createCategory,
  updateCategory,
  deleteCategory,
} from "@/lib/api/categories";
import { extractErrorMessage } from "@/lib/api/client";
import type { Category } from "@/types/category";

export default function AdminCategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  // Modal States
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [nameInput, setNameInput] = useState("");
  const [descInput, setDescInput] = useState("");
  const [submitting, setSubmitting] = useState(false);

  // Delete State
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const loadData = () => {
    setLoading(true);
    getCategories()
      .then((data) => {
        setCategories(data);
        setLoading(false);
      })
      .catch((err) => {
        setActionError(extractErrorMessage(err));
        setLoading(false);
      });
  };

  useEffect(() => {
    loadData();
  }, []);

  const openCreateModal = () => {
    setNameInput("");
    setDescInput("");
    setActionError(null);
    setActionSuccess(null);
    setIsCreateOpen(true);
  };

  const openEditModal = (cat: Category) => {
    setEditingCategory(cat);
    setNameInput(cat.name);
    setDescInput(cat.description || "");
    setActionError(null);
    setActionSuccess(null);
  };

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!nameInput.trim()) {
      setActionError("Tên danh mục không được để trống.");
      return;
    }
    setSubmitting(true);
    setActionError(null);
    try {
      await createCategory({
        name: nameInput.trim(),
        description: descInput.trim() || null,
      });
      setActionSuccess(`Đã tạo danh mục "${nameInput.trim()}" thành công!`);
      setIsCreateOpen(false);
      loadData();
    } catch (err) {
      setActionError(extractErrorMessage(err, "Không thể tạo danh mục. Vui lòng kiểm tra quyền Admin hoặc tên có bị trùng."));
    } finally {
      setSubmitting(false);
    }
  };

  const handleUpdateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingCategory || !nameInput.trim()) return;
    setSubmitting(true);
    setActionError(null);
    try {
      await updateCategory(editingCategory.id, {
        name: nameInput.trim(),
        description: descInput.trim() || null,
      });
      setActionSuccess(`Đã cập nhật danh mục "${nameInput.trim()}" thành công!`);
      setEditingCategory(null);
      loadData();
    } catch (err) {
      setActionError(extractErrorMessage(err, "Không thể cập nhật danh mục."));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (cat: Category) => {
    if (!confirm(`Bạn có chắc muốn xóa danh mục "${cat.name}"?`)) return;
    setDeletingId(cat.id);
    setActionError(null);
    setActionSuccess(null);
    try {
      await deleteCategory(cat.id);
      setActionSuccess(`Đã xóa danh mục "${cat.name}" thành công!`);
      loadData();
    } catch (err) {
      // Handles 409 Conflict (CATEGORY_DELETE_HAS_RECIPES)
      setActionError(
        extractErrorMessage(
          err,
          `Không thể xóa danh mục "${cat.name}" vì vẫn còn công thức nấu ăn trực thuộc (409 Conflict - CATEGORY_DELETE_HAS_RECIPES).`
        )
      );
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <main className="min-h-screen bg-gray-50/60 py-10 px-4 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-6xl space-y-6">
        {/* Header (Screen 11 in Design) */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 rounded-3xl border border-gray-100 shadow-xs">
          <div>
            <div className="flex items-center gap-2">
              <span className="h-2 w-2 rounded-full bg-emerald-600" />
              <span className="text-xs font-bold text-[#0d5c3a] uppercase tracking-wider">
                Quản trị hệ thống (Admin)
              </span>
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mt-1">
              Quản lý danh mục công thức
            </h1>
            <p className="text-xs text-gray-500">
              Thêm mới, cập nhật hoặc xóa danh mục món ăn (Chỉ dành cho Admin).
            </p>
          </div>

          <div className="flex items-center gap-2">
            <Link
              href="/dashboard/statistics"
              className="px-3.5 py-2 rounded-xl border border-gray-200 text-xs font-semibold text-gray-700 hover:bg-gray-50 transition-colors"
            >
              📊 Báo cáo thống kê
            </Link>
            <button
              onClick={openCreateModal}
              className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-[#0d5c3a] hover:bg-[#094229] text-xs font-bold text-white shadow-xs transition-colors cursor-pointer"
            >
              <span>+</span>
              <span>Thêm danh mục</span>
            </button>
          </div>
        </div>

        {/* Action Notifications */}
        {actionSuccess && (
          <div className="rounded-2xl bg-emerald-50 border border-emerald-200 p-4 text-xs font-semibold text-emerald-800 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span>✅</span>
              <span>{actionSuccess}</span>
            </div>
            <button onClick={() => setActionSuccess(null)} className="text-emerald-600 hover:text-emerald-900">
              ✕
            </button>
          </div>
        )}

        {actionError && (
          <div className="rounded-2xl bg-rose-50 border border-rose-200 p-4 text-xs font-semibold text-rose-800 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="text-base">⚠️</span>
              <span>{actionError}</span>
            </div>
            <button onClick={() => setActionError(null)} className="text-rose-600 hover:text-rose-900">
              ✕
            </button>
          </div>
        )}

        {/* Categories Table */}
        <div className="overflow-hidden rounded-3xl bg-white border border-gray-100 shadow-xs">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs text-gray-600">
              <thead className="bg-gray-50/80 text-[11px] font-bold uppercase tracking-wider text-gray-500 border-b border-gray-100">
                <tr>
                  <th className="py-3.5 px-6">Tên danh mục</th>
                  <th className="py-3.5 px-4">Slug định danh</th>
                  <th className="py-3.5 px-6">Mô tả</th>
                  <th className="py-3.5 px-4 text-center">Số công thức</th>
                  <th className="py-3.5 px-6 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {loading ? (
                  <tr>
                    <td colSpan={5} className="py-12 text-center text-gray-400">
                      Đang tải danh sách danh mục...
                    </td>
                  </tr>
                ) : categories.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-12 text-center text-gray-400">
                      Chưa có danh mục nào trong hệ thống.
                    </td>
                  </tr>
                ) : (
                  categories.map((cat) => (
                    <tr key={cat.id} className="hover:bg-emerald-50/20 transition-colors">
                      <td className="py-4 px-6 font-bold text-gray-900">
                        <div className="flex items-center gap-2">
                          <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-emerald-100 text-[#0d5c3a] font-bold text-xs">
                            {cat.name[0]}
                          </span>
                          <span>{cat.name}</span>
                        </div>
                      </td>
                      <td className="py-4 px-4 font-mono text-gray-500 text-[11px]">
                        {cat.slug}
                      </td>
                      <td className="py-4 px-6 text-gray-600 max-w-xs truncate">
                        {cat.description || "—"}
                      </td>
                      <td className="py-4 px-4 text-center">
                        <span className={`inline-block rounded-full px-2.5 py-0.5 text-xs font-bold ${
                          cat.recipeCount > 0 ? "bg-emerald-100 text-[#0d5c3a]" : "bg-gray-100 text-gray-500"
                        }`}>
                          {cat.recipeCount} món
                        </span>
                      </td>
                      <td className="py-4 px-6 text-right space-x-2">
                        <button
                          onClick={() => openEditModal(cat)}
                          className="px-2.5 py-1 rounded-lg border border-gray-200 text-gray-700 hover:bg-gray-50 font-semibold cursor-pointer"
                        >
                          Sửa
                        </button>
                        <button
                          disabled={deletingId === cat.id}
                          onClick={() => handleDelete(cat)}
                          className="px-2.5 py-1 rounded-lg bg-rose-50 text-rose-600 hover:bg-rose-100 font-semibold cursor-pointer disabled:opacity-50"
                          title={cat.recipeCount > 0 ? "Danh mục còn công thức sẽ bị từ chối với lỗi 409" : "Xóa danh mục"}
                        >
                          {deletingId === cat.id ? "Đang xóa..." : "Xóa"}
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* ── CREATE CATEGORY MODAL ─────────────────────────────────────── */}
      {isCreateOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl border border-gray-100 space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <h3 className="text-base font-bold text-gray-900">Tạo danh mục mới</h3>
              <button onClick={() => setIsCreateOpen(false)} className="text-gray-400 hover:text-gray-600 p-1">
                ✕
              </button>
            </div>

            <form onSubmit={handleCreateSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-700 mb-1">
                  Tên danh mục <span className="text-rose-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  value={nameInput}
                  onChange={(e) => setNameInput(e.target.value)}
                  placeholder="Ví dụ: Món nướng BBQ, Món chay..."
                  className="w-full rounded-xl border border-gray-200 p-2.5 text-xs text-gray-900 focus:border-[#0d5c3a] focus:ring-1 focus:ring-[#0d5c3a] outline-none"
                />
                <span className="text-[11px] text-gray-400 mt-1 block">
                  Slug URL sẽ được tự động sinh từ tên và thêm hậu tố nếu trùng lặp.
                </span>
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-700 mb-1">
                  Mô tả danh mục
                </label>
                <textarea
                  rows={3}
                  value={descInput}
                  onChange={(e) => setDescInput(e.target.value)}
                  placeholder="Mô tả ngắn gọn về danh mục..."
                  className="w-full rounded-xl border border-gray-200 p-2.5 text-xs text-gray-900 focus:border-[#0d5c3a] focus:ring-1 focus:ring-[#0d5c3a] outline-none"
                />
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setIsCreateOpen(false)}
                  className="px-4 py-2 text-xs font-medium text-gray-600 hover:bg-gray-100 rounded-xl"
                >
                  Hủy bỏ
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2 text-xs font-bold text-white bg-[#0d5c3a] hover:bg-[#094229] rounded-xl shadow-xs disabled:opacity-50 cursor-pointer"
                >
                  {submitting ? "Đang tạo..." : "Tạo danh mục"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── EDIT CATEGORY MODAL ───────────────────────────────────────── */}
      {editingCategory && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl border border-gray-100 space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <h3 className="text-base font-bold text-gray-900">
                Chỉnh sửa danh mục ({editingCategory.slug})
              </h3>
              <button onClick={() => setEditingCategory(null)} className="text-gray-400 hover:text-gray-600 p-1">
                ✕
              </button>
            </div>

            <form onSubmit={handleUpdateSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-700 mb-1">
                  Tên danh mục mới <span className="text-rose-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  value={nameInput}
                  onChange={(e) => setNameInput(e.target.value)}
                  className="w-full rounded-xl border border-gray-200 p-2.5 text-xs text-gray-900 focus:border-[#0d5c3a] focus:ring-1 focus:ring-[#0d5c3a] outline-none"
                />
                <span className="text-[11px] text-emerald-700 mt-1 block">
                  Lưu ý: Slug sẽ được giữ nguyên theo chuẩn FR-CAT-004 để tránh làm hỏng liên kết cũ.
                </span>
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-700 mb-1">
                  Mô tả danh mục
                </label>
                <textarea
                  rows={3}
                  value={descInput}
                  onChange={(e) => setDescInput(e.target.value)}
                  className="w-full rounded-xl border border-gray-200 p-2.5 text-xs text-gray-900 focus:border-[#0d5c3a] focus:ring-1 focus:ring-[#0d5c3a] outline-none"
                />
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setEditingCategory(null)}
                  className="px-4 py-2 text-xs font-medium text-gray-600 hover:bg-gray-100 rounded-xl"
                >
                  Hủy bỏ
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2 text-xs font-bold text-white bg-[#0d5c3a] hover:bg-[#094229] rounded-xl shadow-xs disabled:opacity-50 cursor-pointer"
                >
                  {submitting ? "Đang lưu..." : "Lưu thay đổi"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </main>
  );
}
