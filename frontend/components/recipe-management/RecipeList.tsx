'use client';

import { useCallback, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch, type ApiError } from '@/lib/api';
import {
  changeRecipeStatus,
  deleteRecipe as deleteRecipeRequest,
  duplicateRecipe,
  getRecipeBySlug,
  getRecipeEtag,
  getRecipes,
  type RecipeSort,
} from '@/lib/api/recipes';
import type { RecipePage, RecipeSummary } from '@/types/recipe';
import RecipeListView from '@/components/recipes/RecipeList';
import ConfirmationDialog from './ConfirmationDialog';

type Category = { id: string; name: string; slug: string; recipeCount: number };

type PendingAction = {
  type: 'publish' | 'unpublish' | 'archive' | 'unarchive' | 'delete';
  recipe: RecipeSummary;
};

type StatusTab = 'all' | 'Vault' | 'Published';

const STATUS_TABS: { key: StatusTab; label: string; icon: string }[] = [
  { key: 'all',       label: 'Tất cả',       icon: '📋' },
  { key: 'Vault',     label: 'Kho lưu trữ', icon: '📦' },
  { key: 'Published', label: 'Đã xuất bản', icon: '🌐' },
];

export default function RecipeList({ admin = false }: { admin?: boolean }) {
  const router = useRouter();
  const [recipes, setRecipes] = useState<RecipePage | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [sort, setSort] = useState<RecipeSort>('newest');
  const [page, setPage] = useState(1);
  const [statusTab, setStatusTab] = useState<StatusTab>(admin ? 'all' : 'Vault');
  const [isTabInitialized, setIsTabInitialized] = useState(admin);
  const [refreshVersion, setRefreshVersion] = useState(0);
  const [error, setError] = useState('');
  const [feedback, setFeedback] = useState('');
  const [busyRecipeId, setBusyRecipeId] = useState<string | null>(null);
  const [busyDuplicateId, setBusyDuplicateId] = useState<string | null>(null);

  // Dialog state
  const [pendingAction, setPendingAction] = useState<PendingAction | null>(null);
  const [isActionLoading, setIsActionLoading] = useState(false);

  useEffect(() => {
    apiFetch<Category[]>('/api/v1/categories')
      .then(setCategories)
      .catch((reason: Error) => setError(reason.message));
  }, []);

  useEffect(() => {
    if (admin) {
      setIsTabInitialized(true);
      return;
    }

    const requestedTab = new URLSearchParams(window.location.search).get('tab');
    if (requestedTab === 'published') setStatusTab('Published');
    else setStatusTab('Vault');
    setIsTabInitialized(true);
  }, [admin]);

  const fetchRecipeList = useCallback(() => {
    if (!isTabInitialized) return;

    getRecipes({
      page,
      pageSize: 6,
      sort,
      search,
      categoryId: categoryId || undefined,
      mine: admin ? undefined : true,
      allStatuses: admin,
      status: !admin && statusTab !== 'all' ? statusTab : undefined,
    })
      .then(setRecipes)
      .catch((reason: Error) => setError(reason.message));
  }, [page, sort, search, categoryId, admin, statusTab, isTabInitialized]);

  useEffect(() => {
    if (isTabInitialized) fetchRecipeList();
  }, [fetchRecipeList, isTabInitialized, refreshVersion]);

  // Execute lifecycle or delete action with fresh ETag
  async function executeAction() {
    if (!pendingAction) return;

    const { type, recipe } = pendingAction;
    setIsActionLoading(true);
    setBusyRecipeId(recipe.id);
    setError('');
    setFeedback('');

    try {
      // Fetch fresh recipe ETag (supports Draft, Archived, and Published recipes)
      const etag = await getRecipeEtag(recipe.id, recipe.slug);

      if (type === 'delete') {
        await deleteRecipeRequest(recipe.id, etag);
        setRecipes((current) =>
          current
            ? {
                ...current,
                items: current.items.filter((item) => item.id !== recipe.id),
                totalCount: Math.max(0, current.totalCount - 1),
              }
            : current,
        );
        setFeedback(`Đã chuyển công thức "${recipe.title}" vào thùng rác.`);
      } else {
        const { data: updatedStatus } = await changeRecipeStatus(recipe.id, type, etag);
        if (!admin) {
          setRefreshVersion((current) => current + 1);
        } else {
          setRecipes((current) =>
            current
              ? {
                  ...current,
                  items: current.items.map((item) =>
                    item.id === recipe.id ? { ...item, status: updatedStatus.status } : item,
                  ),
                }
              : current,
          );
        }
        const actionLabels: Record<string, string> = {
          publish: 'Đã xuất bản công thức lên cộng đồng',
          unpublish: 'Đã hủy xuất bản và chuyển công thức vào Kho lưu trữ',
          archive: 'Đã lưu trữ công thức',
          unarchive: 'Đã mở lưu trữ công thức trong Kho lưu trữ',
        };
        setFeedback(`${actionLabels[type] || 'Đã cập nhật trạng thái'}: "${recipe.title}".`);
      }

      setPendingAction(null);
    } catch (err: unknown) {
      const apiErr = err as ApiError;
      if (apiErr.status === 409 || apiErr.message?.includes('409') || apiErr.message?.includes('thay đổi bởi một yêu cầu khác')) {
        setError(
          'Xung đột phiên bản (409 Conflict): Công thức này đã bị thay đổi đồng thời bởi một phiên làm việc khác. Hãy làm mới danh sách.',
        );
      } else {
        setError(apiErr.message || 'Thao tác không thành công.');
      }
    } finally {
      setIsActionLoading(false);
      setBusyRecipeId(null);
    }
  }

  const totalPages = recipes ? Math.max(1, Math.ceil(recipes.totalCount / recipes.pageSize)) : 1;

  // Dialog configurations based on pendingAction
  const dialogConfig = pendingAction
    ? {
        publish: {
          title: 'Xác nhận xuất bản công thức',
          message: `Bạn có chắc chắn muốn xuất bản công thức "${pendingAction.recipe.title}" lên cộng đồng?`,
          confirmText: 'Xuất bản',
          variant: 'primary' as const,
        },
        unpublish: {
          title: 'Xác nhận hủy xuất bản',
          message: `Bạn có muốn hủy xuất bản công thức "${pendingAction.recipe.title}" và đưa vào Kho lưu trữ không?`,
          confirmText: 'Có',
          cancelText: 'Không',
          variant: 'warning' as const,
        },
        archive: {
          title: 'Xác nhận lưu trữ công thức',
          message: `Lưu trữ công thức "${pendingAction.recipe.title}"? Công thức sẽ bị ẩn khỏi danh sách tìm kiếm và khám phá.`,
          confirmText: 'Lưu trữ',
          variant: 'warning' as const,
        },
        unarchive: {
          title: 'Xác nhận mở lại lưu trữ',
          message: `Mở lại công thức "${pendingAction.recipe.title}" về trạng thái Bản nháp (Draft)?`,
          confirmText: 'Mở lưu trữ',
          variant: 'primary' as const,
        },
        delete: {
          title: 'Xác nhận xóa công thức',
          message: `Bạn chắc chắn muốn chuyển công thức "${pendingAction.recipe.title}" vào thùng rác?`,
          confirmText: 'Xóa công thức',
          variant: 'danger' as const,
        },
      }[pendingAction.type]
    : null;

  return (
    <section className="recipe-workspace">
      {/* ── Status tabs ── */}
      {!admin && (
        <div className="recipe-status-tabs" role="tablist" aria-label="Lọc theo trạng thái">
          {STATUS_TABS.filter((tab) => admin ? tab.key === 'all' : tab.key !== 'all').map((tab) => (
            <button
              key={tab.key}
              role="tab"
              type="button"
              aria-selected={statusTab === tab.key}
              className={`status-tab${statusTab === tab.key ? ' status-tab--active' : ''} status-tab--${tab.key.toLowerCase()}`}
              onClick={() => {
                setStatusTab(tab.key);
                setPage(1);
              }}
            >
              <span className="status-tab-icon">{tab.icon}</span>
              <span className="status-tab-label">{tab.label}</span>
              {statusTab === tab.key && recipes && (
                <span className="status-tab-count">{recipes.totalCount}</span>
              )}
            </button>
          ))}
        </div>
      )}

      {!admin && statusTab === 'Vault' && (
        <div className="status-tab-notice status-tab-notice--draft">
          <span>📦</span>
          <div>
            <strong>Kho lưu trữ</strong> — Gồm bản nháp, bản sao chép và công thức đã lưu trữ.
            Bạn có thể chỉnh sửa và lưu tại đây; công thức chỉ xuất hiện công khai sau khi nhấn <strong>Xuất bản</strong>.
          </div>
        </div>
      )}

      <div className="recipe-toolbar">
        <label className="field field-search" htmlFor="recipe-search-input">
          <span>Tìm công thức</span>
          <input
            id="recipe-search-input"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
            placeholder="Phở, mì, món chay..."
          />
        </label>
        <label className="field" htmlFor="recipe-category-select">
          <span>Danh mục</span>
          <select
            id="recipe-category-select"
            value={categoryId}
            onChange={(event) => {
              setCategoryId(event.target.value);
              setPage(1);
            }}
          >
            <option value="">Tất cả danh mục</option>
            {Array.isArray(categories) && categories.map((category, idx) => (
              <option key={category.id || ("cat-" + idx)} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        <label className="field" htmlFor="recipe-sort-select">
          <span>Sắp xếp</span>
          <select
            id="recipe-sort-select"
            value={sort}
            onChange={(event) => {
              const value = event.target.value;
              if (
                value === 'newest' ||
                value === 'oldest' ||
                value === 'title' ||
                value === 'cooktime'
              ) {
                setSort(value);
              }
              setPage(1);
            }}
          >
            <option value="newest">Mới nhất</option>
            <option value="oldest">Cũ nhất</option>
            <option value="title">Tên món</option>
            <option value="cooktime">Thời gian nấu</option>
          </select>
        </label>
      </div>

      {error && (
        <div className="form-error" role="alert" style={{ marginBottom: 16 }}>
          <span>❌ {error}</span>
          <button
            type="button"
            className="small-button"
            style={{ marginLeft: 12 }}
            onClick={fetchRecipeList}
          >
            Làm mới danh sách
          </button>
        </div>
      )}

      {feedback && (
        <div className="form-success-banner" role="status" style={{ marginBottom: 16 }}>
          <span>✅ {feedback}</span>
        </div>
      )}

      <div className="recipe-list-heading">
        <div>
          <strong>{recipes?.totalCount ?? 0}</strong>
          {' '}
          {statusTab === 'Vault' ? 'công thức trong Kho lưu trữ'
            : statusTab === 'Published' ? 'công thức đã xuất bản'
            : 'công thức phù hợp'}
        </div>
        <a className="primary-button" href="/recipes/new">
          + Tạo công thức mới
        </a>
      </div>

      {recipes && (
        <RecipeListView
          recipes={recipes.items}
          emptyMessage={
            statusTab === 'Vault'
              ? 'Kho lưu trữ đang trống. Bản nháp và bản sao sẽ xuất hiện ở đây để bạn chỉnh sửa trước khi xuất bản.'
              : 'Chưa có công thức phù hợp.'
          }
          showStatus
          renderActions={(recipe) => {
            const isBusy = busyRecipeId === recipe.id;
            const isDuplicating = busyDuplicateId === recipe.id;
            return (
              <div className="card-actions-row">
                {recipe.status === 'Published' ? (
                  <button
                    className="card-action card-action-published"
                    type="button"
                    disabled={isBusy}
                    onClick={() => setPendingAction({ type: 'unpublish', recipe })}
                    title="Nhấn để hủy xuất bản và đưa công thức vào Kho lưu trữ"
                  >
                    {isBusy ? '⏳ Đang xử lý...' : '✓ Đã xuất bản'}
                  </button>
                ) : (
                  <button
                    className="card-action card-action-publish"
                    type="button"
                    disabled={isBusy}
                    onClick={() => setPendingAction({ type: 'publish', recipe })}
                    title="Xuất bản công thức"
                  >
                    {isBusy ? '⏳ Đang xử lý...' : '🚀 Xuất bản'}
                  </button>
                )}
                {/* Nút chỉnh sửa chỉ được phép hiển thị trong Kho lưu trữ (Draft / Archived) */}
                {(statusTab === 'Vault' || (recipe.status === 'Draft' || recipe.status === 'Archived')) && (
                  <a
                    className="card-action card-action-edit"
                    href={`/recipes/${encodeURIComponent(recipe.slug)}/edit`}
                  >
                    ✏️ Chỉnh sửa
                  </a>
                )}

                {/* Duplicate button – only shown in author's own recipe list */}
                {!admin && (
                  <button
                    className="card-action card-action-duplicate"
                    type="button"
                    disabled={isBusy || isDuplicating}
                    title="Tạo bản sao công thức này"
                    onClick={() => {
                      setBusyDuplicateId(recipe.id);
                      setError('');
                      duplicateRecipe(recipe.id)
                        .then((result) => {
                          setFeedback(`Đã tạo bản sao "${result.title}" trong Kho lưu trữ. Bạn có thể chỉnh sửa tại đây và chỉ xuất bản khi sẵn sàng.`);
                          setStatusTab('Vault');
                          setPage(1);
                          setRefreshVersion((current) => current + 1);
                          router.replace('/recipes?tab=vault', { scroll: false });
                        })
                        .catch((err: unknown) => {
                          const apiErr = err as ApiError;
                          setError(apiErr.message || 'Không thể sao chép công thức.');
                        })
                        .finally(() => setBusyDuplicateId(null));
                    }}
                  >
                    {isDuplicating ? '⏳ Đang sao chép...' : '🔁 Sao chép'}
                  </button>
                )}



                <button
                  className="delete-action"
                  type="button"
                  disabled={isBusy}
                  onClick={() => setPendingAction({ type: 'delete', recipe })}
                  title="Xóa công thức vào thùng rác"
                >
                  🗑️ Xóa
                </button>
              </div>
            );
          }}
        />
      )}

      <div className="pagination">
        <button
          type="button"
          onClick={() => setPage((current) => Math.max(1, current - 1))}
          disabled={page === 1}
        >
          ← Trước
        </button>
        <span>
          Trang {page} / {totalPages}
        </span>
        <button
          type="button"
          onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
          disabled={page >= totalPages}
        >
          Sau →
        </button>
      </div>

      {/* Confirmation Dialog */}
      {dialogConfig && pendingAction && (
        <ConfirmationDialog
          isOpen={Boolean(pendingAction)}
          title={dialogConfig.title}
          message={dialogConfig.message}
          confirmText={dialogConfig.confirmText}
          cancelText={'cancelText' in dialogConfig ? (dialogConfig as { cancelText?: string }).cancelText : 'Hủy bỏ'}
          variant={dialogConfig.variant}
          isLoading={isActionLoading}
          onConfirm={executeAction}
          onCancel={() => !isActionLoading && setPendingAction(null)}
        />
      )}
    </section>
  );
}
