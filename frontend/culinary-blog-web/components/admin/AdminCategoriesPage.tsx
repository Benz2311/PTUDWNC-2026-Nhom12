/* eslint-disable @next/next/no-img-element */
"use client";

import React, { useState, useEffect, useCallback, useMemo } from "react";
import Link from "next/link";
import {
  Layers,
  Plus,
  Search,
  RotateCcw,
  Pencil,
  Trash2,
  ExternalLink,
  BookOpen,
  AlertCircle,
  ImageIcon,
  Loader2,
  X,
  CheckCircle2,
  Filter,
} from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/components/ui/toast";
import {
  getCategories,
  createCategory,
  updateCategory,
  deleteCategory,
  getCategoryStatistics,
  CategoryConflictError,
} from "@/lib/api/categories";
import type { Category, CategoryStatistics } from "@/types/category";
import ConfirmationModal from "./ConfirmationModal";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";

interface CategoryFormData {
  name: string;
  description: string;
  imageUrl: string;
  orderIndex: number;
}

const initialFormData: CategoryFormData = {
  name: "",
  description: "",
  imageUrl: "",
  orderIndex: 0,
};

export default function AdminCategoriesPage() {
  useAuth();
  const { success, error: showErrorToast } = useToast();

  const [categories, setCategories] = useState<Category[]>([]);
  const [stats, setStats] = useState<CategoryStatistics | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Search & Filter
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState<"order" | "name" | "recipes">("order");

  // Modal Create / Edit
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [formData, setFormData] = useState<CategoryFormData>(initialFormData);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formConflictError, setFormConflictError] = useState<string | null>(null);

  // Modal Delete
  const [deletingCategory, setDeletingCategory] = useState<Category | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  // Fetch categories & statistics
  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [catList, statData] = await Promise.all([
        getCategories(),
        getCategoryStatistics(),
      ]);
      setCategories(catList);
      setStats(statData);
    } catch (err: any) {
      console.error("Failed to load category data:", err);
      setError("Không thể tải danh sách danh mục. Vui lòng thử lại sau.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Filtered & Sorted Categories
  const filteredCategories = useMemo(() => {
    let result = [...categories];

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase().trim();
      result = result.filter(
        (c) =>
          c.name.toLowerCase().includes(q) ||
          c.slug.toLowerCase().includes(q) ||
          (c.description && c.description.toLowerCase().includes(q))
      );
    }

    result.sort((a, b) => {
      if (sortBy === "order") {
        return (a.orderIndex ?? 0) - (b.orderIndex ?? 0);
      }
      if (sortBy === "recipes") {
        return (b.recipeCount ?? 0) - (a.recipeCount ?? 0);
      }
      return a.name.localeCompare(b.name, "vi");
    });

    return result;
  }, [categories, searchQuery, sortBy]);

  // Open Create Modal
  const handleOpenCreateModal = () => {
    setEditingCategory(null);
    setFormData({
      name: "",
      description: "",
      imageUrl: "",
      orderIndex: categories.length > 0 ? Math.max(...categories.map((c) => c.orderIndex ?? 0)) + 1 : 0,
    });
    setFormErrors({});
    setFormConflictError(null);
    setIsFormModalOpen(true);
  };

  // Open Edit Modal
  const handleOpenEditModal = (cat: Category) => {
    setEditingCategory(cat);
    setFormData({
      name: cat.name,
      description: cat.description || "",
      imageUrl: cat.imageUrl || "",
      orderIndex: cat.orderIndex ?? 0,
    });
    setFormErrors({});
    setFormConflictError(null);
    setIsFormModalOpen(true);
  };

  // Validate form
  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};
    if (!formData.name.trim()) {
      errors.name = "Tên danh mục không được để trống.";
    } else if (formData.name.trim().length < 2) {
      errors.name = "Tên danh mục phải có ít nhất 2 ký tự.";
    } else if (formData.name.trim().length > 100) {
      errors.name = "Tên danh mục không được vượt quá 100 ký tự.";
    }

    if (formData.imageUrl.trim() && !/^https?:\/\//i.test(formData.imageUrl.trim())) {
      errors.imageUrl = "Đường dẫn ảnh phải bắt đầu bằng http:// hoặc https://";
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  // Submit Create or Edit
  const handleFormSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    setFormConflictError(null);

    try {
      if (editingCategory) {
        // Update
        await updateCategory(editingCategory.id, {
          name: formData.name.trim(),
          description: formData.description.trim() || null,
          imageUrl: formData.imageUrl.trim() || null,
          orderIndex: Number(formData.orderIndex) || 0,
        });

        // Update local state directly
        setCategories((prev) =>
          prev.map((c) =>
            c.id === editingCategory.id
              ? {
                  ...c,
                  name: formData.name.trim(),
                  description: formData.description.trim() || null,
                  imageUrl: formData.imageUrl.trim() || null,
                  orderIndex: Number(formData.orderIndex) || 0,
                }
              : c
          )
        );

        success("Cập nhật danh mục thành công!");
      } else {
        // Create
        const newCat = await createCategory({
          name: formData.name.trim(),
          description: formData.description.trim() || null,
          imageUrl: formData.imageUrl.trim() || null,
          orderIndex: Number(formData.orderIndex) || 0,
        });

        // Add to local state
        setCategories((prev) => [...prev, newCat]);
        success("Tạo danh mục mới thành công!");
      }

      setIsFormModalOpen(false);
      // Reload stats in background
      getCategoryStatistics().then((s) => setStats(s)).catch(() => {});
    } catch (err: any) {
      if (err instanceof CategoryConflictError || err?.status === 409 || err?.response?.status === 409) {
        const msg = err.message || "Tên danh mục đã tồn tại trong hệ thống.";
        setFormConflictError(msg);
        showErrorToast(msg);
      } else {
        const msg = err?.message || "Đã xảy ra lỗi khi lưu danh mục. Vui lòng thử lại.";
        setFormConflictError(msg);
        showErrorToast(msg);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  // Execute Delete
  const handleConfirmDelete = async () => {
    if (!deletingCategory) return;

    setIsDeleting(true);
    try {
      await deleteCategory(deletingCategory.id);

      // Remove from state
      setCategories((prev) => prev.filter((c) => c.id !== deletingCategory.id));
      success(`Đã xóa danh mục "${deletingCategory.name}" thành công!`);
      setDeletingCategory(null);

      // Reload stats
      getCategoryStatistics().then((s) => setStats(s)).catch(() => {});
    } catch (err: any) {
      if (err instanceof CategoryConflictError || err?.status === 409 || err?.response?.status === 409) {
        const msg =
          deletingCategory.recipeCount > 0
            ? `Không thể xóa danh mục "${deletingCategory.name}" vì vẫn còn ${deletingCategory.recipeCount} công thức liên kết! Vui lòng chuyển hoặc xóa các công thức trước.`
            : "Không thể xóa danh mục vì vẫn còn công thức liên kết.";
        showErrorToast(msg);
      } else {
        showErrorToast(err?.message || "Không thể xóa danh mục vào lúc này.");
      }
    } finally {
      setIsDeleting(false);
    }
  };

  const totalRecipeCount = useMemo(() => {
    return categories.reduce((sum, c) => sum + (c.recipeCount || 0), 0);
  }, [categories]);

  return (
    <div className="container-page py-8 sm:py-10 space-y-8">
      {/* ── BREADCRUMB ────────────────────────────────────────────── */}
      <nav aria-label="Breadcrumb" className="text-xs text-gray-500 flex items-center gap-2">
        <Link href="/" className="hover:text-emerald-700 transition-colors">
          Trang chủ
        </Link>
        <span>/</span>
        <span className="text-gray-500">Quản trị</span>
        <span>/</span>
        <span className="text-gray-900 font-semibold">Quản lý danh mục</span>
      </nav>

      {/* ── HEADER BANNER ─────────────────────────────────────────── */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 sm:p-8 rounded-3xl bg-gradient-to-r from-emerald-800 via-emerald-700 to-teal-900 text-white shadow-md">
        <div className="space-y-2">
          <div className="inline-flex items-center gap-2 rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-emerald-100 border border-white/15">
            <Layers className="h-4 w-4 text-amber-300" />
            <span>Phân hệ Quản trị Admin</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight">
            Quản lý danh mục công thức
          </h1>
          <p className="text-xs sm:text-sm text-emerald-100/90 max-w-2xl">
            Tạo mới, chỉnh sửa thông tin, sắp xếp thứ tự hiển thị và quản lý phân loại món ăn trong toàn hệ thống.
          </p>
        </div>

        <div className="flex items-center gap-3 self-start md:self-center">
          <Button
            onClick={loadData}
            variant="outline"
            size="sm"
            disabled={isLoading}
            className="border-white/20 bg-white/10 text-white hover:bg-white/20 hover:text-white"
          >
            <RotateCcw className={`h-4 w-4 mr-1.5 ${isLoading ? "animate-spin" : ""}`} />
            Làm mới
          </Button>
          <Button
            onClick={handleOpenCreateModal}
            size="sm"
            className="bg-amber-500 hover:bg-amber-600 text-emerald-950 font-bold shadow-sm"
          >
            <Plus className="h-4 w-4 mr-1.5" />
            Thêm danh mục
          </Button>
        </div>
      </div>

      {/* ── STATS ROW ─────────────────────────────────────────────── */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Card className="p-5 border-emerald-100 bg-white shadow-xs flex items-center gap-4">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-800 shrink-0">
            <Layers className="h-6 w-6" />
          </div>
          <div>
            <div className="text-xs font-medium text-gray-500">Tổng số danh mục</div>
            <div className="text-2xl font-extrabold text-gray-900 mt-0.5">
              {isLoading ? "—" : categories.length}
            </div>
          </div>
        </Card>

        <Card className="p-5 border-emerald-100 bg-white shadow-xs flex items-center gap-4">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-100 text-amber-800 shrink-0">
            <BookOpen className="h-6 w-6" />
          </div>
          <div>
            <div className="text-xs font-medium text-gray-500">Tổng số công thức liên kết</div>
            <div className="text-2xl font-extrabold text-gray-900 mt-0.5">
              {isLoading ? "—" : totalRecipeCount}
            </div>
          </div>
        </Card>

        <Card className="p-5 border-emerald-100 bg-white shadow-xs flex items-center gap-4">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-100 text-blue-800 shrink-0">
            <CheckCircle2 className="h-6 w-6" />
          </div>
          <div>
            <div className="text-xs font-medium text-gray-500">Công thức đã xuất bản</div>
            <div className="text-2xl font-extrabold text-gray-900 mt-0.5">
              {isLoading ? "—" : stats?.publishedRecipes ?? "—"}
            </div>
          </div>
        </Card>
      </div>

      {/* ── CONTROLS & SEARCH BAR ─────────────────────────────────── */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-white p-4 rounded-2xl border border-gray-200 shadow-xs">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400 pointer-events-none" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Tìm theo tên, mô tả hoặc slug..."
            className="w-full h-10 pl-9 pr-9 text-xs rounded-xl border border-gray-200 bg-gray-50 focus:bg-white focus:border-emerald-600 focus:outline-none focus:ring-2 focus:ring-emerald-600/20 transition-all"
          />
          {searchQuery && (
            <button
              onClick={() => setSearchQuery("")}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-gray-400 hidden sm:block" />
          <span className="text-xs text-gray-500 whitespace-nowrap">Sắp xếp:</span>
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as any)}
            className="h-10 text-xs rounded-xl border border-gray-200 bg-gray-50 px-3 text-gray-700 focus:bg-white focus:border-emerald-600 focus:outline-none"
          >
            <option value="order">Theo thứ tự hiển thị (OrderIndex)</option>
            <option value="name">Theo tên danh mục (A - Z)</option>
            <option value="recipes">Theo số lượng công thức (Giảm dần)</option>
          </select>
        </div>
      </div>

      {/* ── ERROR ALERT ───────────────────────────────────────────── */}
      {error && (
        <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-xs text-red-700 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertCircle className="h-4 w-4 text-red-600 shrink-0" />
            <span>{error}</span>
          </div>
          <Button size="sm" variant="outline" onClick={loadData} className="text-xs">
            Thử lại
          </Button>
        </div>
      )}

      {/* ── DATA TABLE & MOBILE CARDS ──────────────────────────────── */}
      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3, 4, 5].map((i) => (
            <div key={i} className="h-16 rounded-2xl bg-gray-100 animate-pulse" />
          ))}
        </div>
      ) : filteredCategories.length === 0 ? (
        <Card className="p-12 text-center border-dashed border-2 border-gray-200 bg-white">
          <Layers className="h-12 w-12 text-gray-300 mx-auto mb-3" />
          <h3 className="text-base font-bold text-gray-800">
            {searchQuery ? "Không tìm thấy danh mục phù hợp" : "Chưa có danh mục nào"}
          </h3>
          <p className="text-xs text-gray-500 mt-1 max-w-sm mx-auto">
            {searchQuery
              ? `Không có kết quả nào khớp với "${searchQuery}". Hãy thử từ khóa khác.`
              : "Bắt đầu thêm danh mục mới để phân loại các công thức nấu ăn trong blog."}
          </p>
          <div className="mt-4">
            {searchQuery ? (
              <Button size="sm" variant="outline" onClick={() => setSearchQuery("")}>
                Xóa bộ lọc tìm kiếm
              </Button>
            ) : (
              <Button size="sm" onClick={handleOpenCreateModal} className="bg-emerald-700 hover:bg-emerald-800 text-white font-semibold">
                <Plus className="h-4 w-4 mr-1.5" />
                Tạo danh mục đầu tiên
              </Button>
            )}
          </div>
        </Card>
      ) : (
        <div className="space-y-4">
          {/* Desktop Table View */}
          <div className="hidden md:block overflow-hidden rounded-2xl border border-gray-200 bg-white shadow-xs">
            <table className="w-full text-left text-xs text-gray-600">
              <thead className="bg-gray-50/80 text-[11px] uppercase tracking-wider text-gray-500 font-semibold border-b border-gray-200">
                <tr>
                  <th scope="col" className="px-4 py-3.5 w-16 text-center">Thứ tự</th>
                  <th scope="col" className="px-4 py-3.5 w-20">Ảnh</th>
                  <th scope="col" className="px-4 py-3.5">Tên danh mục & Slug</th>
                  <th scope="col" className="px-4 py-3.5">Mô tả</th>
                  <th scope="col" className="px-4 py-3.5 text-center w-28">Số công thức</th>
                  <th scope="col" className="px-4 py-3.5 text-right w-36">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filteredCategories.map((cat) => (
                  <tr key={cat.id} className="hover:bg-emerald-50/40 transition-colors">
                    {/* Order Index */}
                    <td className="px-4 py-3.5 text-center font-mono font-bold text-gray-700">
                      <span className="inline-flex items-center justify-center w-7 h-7 rounded-lg bg-gray-100 text-gray-800 text-xs">
                        {cat.orderIndex ?? 0}
                      </span>
                    </td>

                    {/* Thumbnail */}
                    <td className="px-4 py-3.5">
                      <div className="relative h-12 w-12 rounded-xl overflow-hidden bg-gray-100 border border-gray-200 flex items-center justify-center">
                        {cat.imageUrl ? (
                          <img
                            src={cat.imageUrl}
                            alt={cat.name}
                            className="h-full w-full object-cover"
                            onError={(e) => {
                              (e.target as HTMLImageElement).style.display = "none";
                            }}
                          />
                        ) : (
                          <ImageIcon className="h-5 w-5 text-gray-400" />
                        )}
                      </div>
                    </td>

                    {/* Name & Slug */}
                    <td className="px-4 py-3.5">
                      <div className="font-bold text-sm text-gray-900">{cat.name}</div>
                      <div className="font-mono text-[11px] text-gray-400">/{cat.slug}</div>
                    </td>

                    {/* Description */}
                    <td className="px-4 py-3.5 max-w-xs truncate text-gray-600">
                      {cat.description || <span className="text-gray-300 italic">Chưa có mô tả</span>}
                    </td>

                    {/* Recipe Count */}
                    <td className="px-4 py-3.5 text-center">
                      <span
                        className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold ${
                          cat.recipeCount > 0
                            ? "bg-emerald-100 text-emerald-800"
                            : "bg-gray-100 text-gray-500"
                        }`}
                      >
                        <BookOpen className="h-3 w-3" />
                        {cat.recipeCount} món
                      </span>
                    </td>

                    {/* Actions */}
                    <td className="px-4 py-3.5 text-right space-x-1">
                      <Link
                        href={`/categories/${cat.slug}`}
                        target="_blank"
                        className="inline-flex items-center justify-center h-8 w-8 rounded-lg text-gray-500 hover:text-emerald-700 hover:bg-emerald-50 transition-colors"
                        title="Xem trang công khai"
                      >
                        <ExternalLink className="h-4 w-4" />
                      </Link>
                      <button
                        onClick={() => handleOpenEditModal(cat)}
                        className="inline-flex items-center justify-center h-8 w-8 rounded-lg text-gray-500 hover:text-blue-700 hover:bg-blue-50 transition-colors"
                        title="Chỉnh sửa danh mục"
                      >
                        <Pencil className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setDeletingCategory(cat)}
                        className="inline-flex items-center justify-center h-8 w-8 rounded-lg text-gray-500 hover:text-red-700 hover:bg-red-50 transition-colors"
                        title="Xóa danh mục"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile Card List View */}
          <div className="grid grid-cols-1 gap-3 md:hidden">
            {filteredCategories.map((cat) => (
              <Card key={cat.id} className="p-4 border-gray-200 bg-white space-y-3">
                <div className="flex items-center gap-3">
                  <div className="relative h-14 w-14 rounded-xl overflow-hidden bg-gray-100 border border-gray-200 shrink-0 flex items-center justify-center">
                    {cat.imageUrl ? (
                      <img
                        src={cat.imageUrl}
                        alt={cat.name}
                        className="h-full w-full object-cover"
                        onError={(e) => {
                          (e.target as HTMLImageElement).style.display = "none";
                        }}
                      />
                    ) : (
                      <ImageIcon className="h-6 w-6 text-gray-400" />
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-1">
                      <h4 className="font-bold text-sm text-gray-900 truncate">{cat.name}</h4>
                      <span className="text-[10px] font-mono px-2 py-0.5 rounded-md bg-gray-100 text-gray-600 shrink-0">
                        #{cat.orderIndex ?? 0}
                      </span>
                    </div>
                    <p className="font-mono text-[11px] text-gray-400 truncate">/{cat.slug}</p>
                    <div className="mt-1">
                      <span className="text-[11px] font-semibold text-emerald-700">
                        {cat.recipeCount} công thức
                      </span>
                    </div>
                  </div>
                </div>

                {cat.description && (
                  <p className="text-xs text-gray-600 line-clamp-2">{cat.description}</p>
                )}

                <div className="flex items-center justify-end gap-2 pt-2 border-t border-gray-100">
                  <Link
                    href={`/categories/${cat.slug}`}
                    target="_blank"
                    className="inline-flex items-center gap-1 px-2.5 py-1.5 text-xs text-gray-600 hover:text-emerald-700 hover:bg-emerald-50 rounded-lg transition-colors"
                  >
                    <ExternalLink className="h-3.5 w-3.5" />
                    <span>Xem</span>
                  </Link>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => handleOpenEditModal(cat)}
                    className="text-xs h-8"
                  >
                    <Pencil className="h-3.5 w-3.5 mr-1 text-blue-600" />
                    Sửa
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => setDeletingCategory(cat)}
                    className="text-xs h-8 text-red-600 border-red-200 hover:bg-red-50 hover:text-red-700"
                  >
                    <Trash2 className="h-3.5 w-3.5 mr-1" />
                    Xóa
                  </Button>
                </div>
              </Card>
            ))}
          </div>
        </div>
      )}

      {/* ── CREATE / EDIT CATEGORY MODAL ──────────────────────────── */}
      {isFormModalOpen && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 flex items-center justify-center p-4"
        >
          {/* Backdrop */}
          <div
            className="absolute inset-0 bg-black/50 backdrop-blur-xs"
            onClick={() => !isSubmitting && setIsFormModalOpen(false)}
          />

          {/* Modal Container */}
          <div className="relative w-full max-w-lg rounded-3xl bg-white p-6 sm:p-7 shadow-2xl border border-gray-100 space-y-5 animate-in fade-in zoom-in-95">
            <div className="flex items-center justify-between pb-3 border-b border-gray-100">
              <div className="flex items-center gap-2">
                <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-100 text-emerald-800">
                  {editingCategory ? <Pencil className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
                </div>
                <div>
                  <h3 className="text-base font-bold text-gray-900">
                    {editingCategory ? "Cập nhật danh mục" : "Tạo danh mục mới"}
                  </h3>
                  <p className="text-[11px] text-gray-500">
                    {editingCategory
                      ? `Đang chỉnh sửa: ${editingCategory.name}`
                      : "Điền thông tin danh mục công thức nấu ăn mới"}
                  </p>
                </div>
              </div>

              <button
                type="button"
                onClick={() => !isSubmitting && setIsFormModalOpen(false)}
                className="text-gray-400 hover:text-gray-600 p-1.5 rounded-xl hover:bg-gray-100 transition-colors"
              >
                <X className="h-5 w-5" />
              </button>
            </div>

            {formConflictError && (
              <div className="rounded-xl border border-red-200 bg-red-50 p-3.5 text-xs text-red-700 flex items-start gap-2.5">
                <AlertCircle className="h-4 w-4 text-red-600 shrink-0 mt-0.5" />
                <div>
                  <div className="font-bold">Lỗi xung đột dữ liệu (409 Conflict)</div>
                  <div>{formConflictError}</div>
                </div>
              </div>
            )}

            <form onSubmit={handleFormSubmit} className="space-y-4">
              {/* Name Field */}
              <div>
                <label className="block text-xs font-bold text-gray-700 mb-1">
                  Tên danh mục <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  value={formData.name}
                  onChange={(e) => {
                    setFormData({ ...formData, name: e.target.value });
                    if (formErrors.name) setFormErrors({ ...formErrors, name: "" });
                  }}
                  placeholder="Ví dụ: Món Việt, Bánh ngọt, Đồ uống thanh nhiệt..."
                  className={`w-full h-10 px-3 text-xs rounded-xl border ${
                    formErrors.name ? "border-red-400 bg-red-50/30" : "border-gray-200 bg-gray-50"
                  } focus:bg-white focus:border-emerald-600 focus:outline-none focus:ring-2 focus:ring-emerald-600/20`}
                />
                {formErrors.name && (
                  <p className="mt-1 text-[11px] text-red-600">{formErrors.name}</p>
                )}
              </div>

              {/* Description Field */}
              <div>
                <label className="block text-xs font-bold text-gray-700 mb-1">
                  Mô tả danh mục
                </label>
                <textarea
                  rows={3}
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Giới thiệu đôi nét về danh mục món ăn này..."
                  className="w-full p-3 text-xs rounded-xl border border-gray-200 bg-gray-50 focus:bg-white focus:border-emerald-600 focus:outline-none focus:ring-2 focus:ring-emerald-600/20"
                />
              </div>

              {/* Image URL & Preview */}
              <div>
                <label className="block text-xs font-bold text-gray-700 mb-1">
                  Đường dẫn ảnh đại diện (ImageUrl)
                </label>
                <input
                  type="url"
                  value={formData.imageUrl}
                  onChange={(e) => {
                    setFormData({ ...formData, imageUrl: e.target.value });
                    if (formErrors.imageUrl) setFormErrors({ ...formErrors, imageUrl: "" });
                  }}
                  placeholder="https://images.unsplash.com/photo-..."
                  className={`w-full h-10 px-3 text-xs rounded-xl border ${
                    formErrors.imageUrl ? "border-red-400 bg-red-50/30" : "border-gray-200 bg-gray-50"
                  } focus:bg-white focus:border-emerald-600 focus:outline-none focus:ring-2 focus:ring-emerald-600/20`}
                />
                {formErrors.imageUrl && (
                  <p className="mt-1 text-[11px] text-red-600">{formErrors.imageUrl}</p>
                )}

                {/* Preview Thumbnail if URL entered */}
                {formData.imageUrl && !formErrors.imageUrl && (
                  <div className="mt-2 flex items-center gap-3 p-2 rounded-xl bg-gray-50 border border-gray-200">
                    <div className="relative h-12 w-20 rounded-lg overflow-hidden bg-gray-200 shrink-0">
                      <img
                        src={formData.imageUrl}
                        alt="Preview"
                        className="h-full w-full object-cover"
                        onError={(e) => {
                          (e.target as HTMLImageElement).src =
                            "https://images.unsplash.com/photo-1498837167922-ddd27525d352?auto=format&fit=crop&w=400&q=80";
                        }}
                      />
                    </div>
                    <div className="text-[11px] text-gray-500 truncate flex-1">
                      Xem trước ảnh đại diện danh mục
                    </div>
                  </div>
                )}
              </div>

              {/* Order Index */}
              <div>
                <label className="block text-xs font-bold text-gray-700 mb-1">
                  Thứ tự sắp xếp hiển thị (OrderIndex)
                </label>
                <input
                  type="number"
                  min="0"
                  value={formData.orderIndex}
                  onChange={(e) =>
                    setFormData({ ...formData, orderIndex: parseInt(e.target.value, 10) || 0 })
                  }
                  className="w-32 h-10 px-3 text-xs rounded-xl border border-gray-200 bg-gray-50 focus:bg-white focus:border-emerald-600 focus:outline-none"
                />
                <span className="ml-2 text-[11px] text-gray-400">
                  Số nhỏ hơn hiển thị trước
                </span>
              </div>

              {/* Action Buttons */}
              <div className="flex items-center justify-end gap-2 pt-3 border-t border-gray-100">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={isSubmitting}
                  onClick={() => setIsFormModalOpen(false)}
                >
                  Hủy
                </Button>
                <Button
                  type="submit"
                  size="sm"
                  disabled={isSubmitting}
                  className="bg-emerald-700 hover:bg-emerald-800 text-white font-bold"
                >
                  {isSubmitting ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-1.5 animate-spin" />
                      Đang lưu...
                    </>
                  ) : editingCategory ? (
                    "Cập nhật danh mục"
                  ) : (
                    "Tạo danh mục"
                  )}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── DELETE CONFIRMATION MODAL ─────────────────────────────── */}
      {deletingCategory && (
        <ConfirmationModal
          isOpen
          title="Xác nhận xóa danh mục"
          message={
            <div className="space-y-3">
              <p>
                Bạn có chắc chắn muốn xóa danh mục{" "}
                <strong className="text-gray-900 font-bold">&quot;{deletingCategory.name}&quot;</strong>?
              </p>
              {deletingCategory.recipeCount > 0 ? (
                <div className="rounded-xl border border-amber-300 bg-amber-50 p-3 text-xs text-amber-900 space-y-1">
                  <div className="font-bold flex items-center gap-1.5 text-amber-800">
                    <AlertCircle className="h-4 w-4 shrink-0 text-amber-600" />
                    <span>Cảnh báo: Danh mục đang có {deletingCategory.recipeCount} công thức!</span>
                  </div>
                  <p className="text-[11px] leading-relaxed">
                    Theo quy định nghiệp vụ SRS, hệ thống sẽ từ chối xóa và trả về lỗi{" "}
                    <strong>409 Conflict</strong> nếu danh mục vẫn còn công thức liên kết chưa được dọn dẹp hoặc chuyển đổi.
                  </p>
                </div>
              ) : (
                <p className="text-xs text-gray-500">
                  Danh mục này hiện không chứa công thức nào. Thao tác xóa sẽ đưa danh mục vào trạng thái xóa mềm.
                </p>
              )}
            </div>
          }
          confirmLabel={isDeleting ? "Đang xóa..." : "Xác nhận xóa"}
          cancelLabel="Hủy"
          variant="danger"
          isLoading={isDeleting}
          onConfirm={handleConfirmDelete}
          onCancel={() => setDeletingCategory(null)}
        />
      )}
    </div>
  );
}

