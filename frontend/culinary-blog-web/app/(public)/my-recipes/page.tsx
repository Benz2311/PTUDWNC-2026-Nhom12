"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { Copy, Pencil, Plus } from "lucide-react";
import { duplicateRecipe, getRecipes } from "@/lib/api/recipes";
import type { PagedResult, RecipeFilterParams, RecipeListItem } from "@/types/recipe";
import RecipeCard from "@/components/recipe/RecipeCard";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { ErrorState } from "@/components/common/ErrorState";
import { EmptyState } from "@/components/common/EmptyState";
import { Pagination } from "@/components/common/Pagination";

type VaultStatus = "Vault" | "Published";

const tabs: { status: VaultStatus; label: string }[] = [
  { status: "Vault", label: "Kho lưu trữ" },
  { status: "Published", label: "Đã xuất bản" },
];

export default function MyRecipesPage() {
  const [status, setStatus] = useState<VaultStatus>("Vault");
  const [isInitialized, setIsInitialized] = useState(false);
  const [page, setPage] = useState(1);
  const [refreshVersion, setRefreshVersion] = useState(0);
  const [recipes, setRecipes] = useState<PagedResult<RecipeListItem>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 12,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState("");
  const [duplicatingId, setDuplicatingId] = useState<string | null>(null);

  useEffect(() => {
    const requestedStatus = new URLSearchParams(window.location.search).get("tab");
    if (requestedStatus === "published") setStatus("Published");
    if (requestedStatus === "drafts" || requestedStatus === "archived") setStatus("Vault");
    setIsInitialized(true);
  }, []);

  const loadRecipes = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params: RecipeFilterParams = {
        mine: true,
        status,
        page,
        pageSize: 12,
        sortBy: "createdAt",
        sortDirection: "desc",
      };
      setRecipes(await getRecipes(params));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Không thể tải Kho công thức của bạn.");
    } finally {
      setLoading(false);
    }
  }, [page, status]);

  useEffect(() => {
    if (isInitialized) loadRecipes();
  }, [isInitialized, loadRecipes, refreshVersion]);

  const selectTab = (nextStatus: VaultStatus) => {
    setStatus(nextStatus);
    setPage(1);
    setFeedback("");
    window.history.replaceState(null, "", `/my-recipes?tab=${nextStatus.toLowerCase()}`);
  };

  const handleDuplicate = async (recipe: RecipeListItem) => {
    setDuplicatingId(recipe.id);
    setError(null);
    try {
      const result = await duplicateRecipe(recipe.id);
      setStatus("Vault");
      setPage(1);
      setFeedback(`Đã tạo bản sao "${result.title}" trong Kho lưu trữ. Bạn có thể chỉnh sửa tại đây và chỉ xuất bản khi sẵn sàng.`);
      window.history.replaceState(null, "", "/my-recipes?tab=vault");
      setRefreshVersion((current) => current + 1);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Không thể sao chép công thức.");
    } finally {
      setDuplicatingId(null);
    }
  };

  const emptyDescription = status === "Vault"
    ? "Bản nháp, bản sao và công thức đã lưu trữ sẽ xuất hiện tại đây. Chỉnh sửa và lưu nội dung; chỉ xuất bản khi bạn chủ động chọn."
    : "Các công thức bạn đã xuất bản sẽ xuất hiện tại đây.";

  return (
    <main className="mx-auto min-h-screen max-w-7xl space-y-6 bg-slate-50/60 px-4 py-8 sm:px-6 lg:px-8">
      <header className="flex flex-col justify-between gap-4 border-b border-gray-200 pb-5 sm:flex-row sm:items-end">
        <div>
          <p className="text-sm font-semibold text-emerald-700">Quản lý nội dung cá nhân</p>
          <h1 className="mt-1 text-3xl font-extrabold text-gray-900">Kho công thức của tôi</h1>
          <p className="mt-2 max-w-2xl text-sm text-gray-600">
            Bản nháp, bản sao và công thức đã lưu trữ được quản lý tại đây. Chỉnh sửa và lưu nội dung; chỉ xuất bản khi bạn chủ động chọn.
          </p>
        </div>
        <Link
          href="/recipes/create"
          className="inline-flex items-center justify-center gap-2 rounded-xl bg-emerald-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-emerald-800"
        >
          <Plus className="h-4 w-4" /> Tạo công thức
        </Link>
      </header>

      <nav aria-label="Các trạng thái công thức" className="flex flex-wrap gap-2">
        {tabs.map((tab) => (
          <button
            key={tab.status}
            type="button"
            role="tab"
            aria-selected={status === tab.status}
            onClick={() => selectTab(tab.status)}
            className={`rounded-full px-4 py-2 text-sm font-semibold transition-colors ${
              status === tab.status
                ? "bg-emerald-700 text-white"
                : "border border-gray-200 bg-white text-gray-700 hover:border-emerald-300 hover:text-emerald-800"
            }`}
          >
            {tab.label}
          </button>
        ))}
        <span className="ml-auto self-center text-sm text-gray-500">
          {recipes.totalCount} công thức
        </span>
      </nav>

      {feedback && (
        <p role="status" className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
          {feedback}
        </p>
      )}

      {loading ? (
        <LoadingSkeleton variant="grid" count={6} />
      ) : error ? (
        <ErrorState message={error} onRetry={loadRecipes} />
      ) : recipes.items.length === 0 ? (
        <EmptyState
          title={status === "Vault" ? "Kho lưu trữ đang trống" : "Chưa có công thức đã xuất bản"}
          description={emptyDescription}
          actionLabel={status === "Vault" ? "Tạo công thức mới" : "Tạo bản nháp"}
          actionHref="/recipes/create"
        />
      ) : (
        <>
          <section aria-label={tabs.find((tab) => tab.status === status)?.label} className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {recipes.items.map((recipe) => (
              <RecipeCard
                key={recipe.id}
                recipe={recipe}
                showStatus
                actions={
                  <div className="flex w-full flex-wrap gap-2">
                    {(status === "Vault" || recipe.status === "Draft" || recipe.status === "Archived") && (
                      <Link
                        href={`/recipes/${encodeURIComponent(recipe.slug)}/edit`}
                        className="inline-flex flex-1 items-center justify-center gap-1.5 rounded-lg bg-emerald-700 px-3 py-2 text-xs font-semibold text-white hover:bg-emerald-800"
                      >
                        <Pencil className="h-3.5 w-3.5" /> Chỉnh sửa
                      </Link>
                    )}
                    <button
                      type="button"
                      disabled={duplicatingId !== null}
                      onClick={() => handleDuplicate(recipe)}
                      className="inline-flex flex-1 items-center justify-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs font-semibold text-slate-700 hover:border-emerald-400 hover:text-emerald-800 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      <Copy className="h-3.5 w-3.5" />
                      {duplicatingId === recipe.id ? "Đang sao chép..." : "Sao chép"}
                    </button>
                  </div>
                }
              />
            ))}
          </section>
          <Pagination
            page={recipes.page}
            totalPages={recipes.totalPages}
            onPageChange={setPage}
          />
        </>
      )}
    </main>
  );
}
