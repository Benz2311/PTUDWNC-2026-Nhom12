'use client';

import { useEffect, useState } from 'react';
import { apiFetch, type ApiError } from '@/lib/api';
import ConfirmationDialog from '@/components/recipe-management/ConfirmationDialog';

type DeletedRecipe = {
  id: string;
  title: string;
  slug: string;
  status: string;
  deletedAt: string;
};

type TrashAction = {
  type: 'restore' | 'purge';
  recipe: DeletedRecipe;
};

export default function RecipeTrash() {
  const [recipes, setRecipes] = useState<DeletedRecipe[]>([]);
  const [error, setError] = useState('');
  const [feedback, setFeedback] = useState('');
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState('');

  // Dialog state
  const [pendingAction, setPendingAction] = useState<TrashAction | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  async function loadTrash() {
    setError('');
    setFeedback('');
    setLoading(true);
    try {
      setRecipes(await apiFetch<DeletedRecipe[]>('/api/v1/admin/recipes/trash'));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không tải được thùng rác.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadTrash();
  }, []);

  async function handleConfirmAction() {
    if (!pendingAction) return;
    const { type, recipe } = pendingAction;

    setBusyId(recipe.id);
    setIsProcessing(true);
    setError('');
    setFeedback('');

    try {
      if (type === 'restore') {
        await apiFetch(`/api/v1/admin/recipes/${recipe.id}/restore`, { method: 'POST' });
        setRecipes((current) => current.filter((item) => item.id !== recipe.id));
        setFeedback(`Đã khôi phục thành công công thức "${recipe.title}".`);
      } else {
        await apiFetch(`/api/v1/admin/recipes/${recipe.id}/purge`, { method: 'DELETE' });
        setRecipes((current) => current.filter((item) => item.id !== recipe.id));
        setFeedback(`Đã xóa vĩnh viễn công thức "${recipe.title}" cùng toàn bộ dữ liệu liên quan.`);
      }
      setPendingAction(null);
    } catch (reason: unknown) {
      const apiErr = reason as ApiError;
      if (apiErr.status === 409) {
        setError('Không thể thực hiện (409 Conflict): Hạn lưu trữ (retention) của công thức đã hết hoặc công thức không ở trạng thái hợp lệ.');
      } else if (apiErr.status === 404) {
        setError('Không tìm thấy công thức trong thùng rác.');
      } else {
        setError(apiErr.message || 'Thao tác không thành công.');
      }
    } finally {
      setIsProcessing(false);
      setBusyId('');
    }
  }

  return (
    <section className="flow-panel" style={{ marginTop: 28 }}>
      <div className="form-actions" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <h2 style={{ margin: 0 }}>Công thức đã xóa (Thùng rác)</h2>
        <button
          className="outline-button"
          type="button"
          onClick={() => void loadTrash()}
          disabled={loading}
        >
          🔄 Làm mới
        </button>
      </div>

      {error && (
        <div className="form-error" role="alert" style={{ margin: '14px 0' }}>
          ❌ {error}
        </div>
      )}

      {feedback && (
        <div className="form-success-banner" role="status" style={{ margin: '14px 0' }}>
          ✅ {feedback}
        </div>
      )}

      {loading ? (
        <p className="loading-state">Đang tải danh sách thùng rác...</p>
      ) : recipes.length === 0 ? (
        <p style={{ color: 'var(--ink-soft)', padding: '24px 0' }}>
          Thùng rác hiện không có công thức nào. Các công thức sau khi xóa mềm sẽ được lưu giữ tại đây tối đa 30 ngày.
        </p>
      ) : (
        <div className="recipe-admin-grid">
          {recipes.map((recipe) => (
            <article className="recipe-admin-card" key={recipe.id}>
              <div className="recipe-admin-card-top">
                <span className="status status-archived">{recipe.status}</span>
                <span style={{ fontSize: 11, color: 'var(--ink-soft)' }}>
                  Đã xóa {new Date(recipe.deletedAt).toLocaleString('vi-VN')}
                </span>
              </div>
              <h3 style={{ margin: '10px 0 16px' }}>{recipe.title}</h3>
              <div className="card-actions">
                <button
                  className="outline-button"
                  type="button"
                  disabled={busyId === recipe.id}
                  onClick={() => setPendingAction({ type: 'restore', recipe })}
                >
                  ♻️ Khôi phục
                </button>
                <button
                  className="delete-action"
                  type="button"
                  disabled={busyId === recipe.id}
                  onClick={() => setPendingAction({ type: 'purge', recipe })}
                >
                  🔥 Xóa vĩnh viễn
                </button>
              </div>
            </article>
          ))}
        </div>
      )}

      {/* Confirmation Dialog for Restore / Purge */}
      {pendingAction && (
        <ConfirmationDialog
          isOpen={Boolean(pendingAction)}
          title={
            pendingAction.type === 'restore'
              ? 'Xác nhận khôi phục công thức'
              : 'Xác nhận xóa vĩnh viễn công thức'
          }
          message={
            pendingAction.type === 'restore' ? (
              <div>
                <p>
                  Khôi phục công thức <strong>&ldquo;{pendingAction.recipe.title}&rdquo;</strong> về trạng thái trước khi xóa?
                </p>
                <p style={{ marginTop: 8, fontSize: 13, color: 'var(--ink-soft)' }}>
                  Công thức sẽ xuất hiện lại trong danh sách quản lý của bạn.
                </p>
              </div>
            ) : (
              <div>
                <p>
                  Bạn có chắc chắn muốn xóa vĩnh viễn công thức <strong>&ldquo;{pendingAction.recipe.title}&rdquo;</strong> cùng toàn bộ ảnh, nguyên liệu và các bước liên quan?
                </p>
                <p style={{ marginTop: 8, fontSize: 13, color: '#b64d35', fontWeight: 600 }}>
                  ⚠️ Cảnh báo: Thao tác này sẽ xóa hoàn toàn khỏi cơ sở dữ liệu và MinIO, không thể hoàn tác!
                </p>
              </div>
            )
          }
          confirmText={pendingAction.type === 'restore' ? 'Khôi phục ngay' : 'Xóa vĩnh viễn'}
          cancelText="Hủy bỏ"
          variant={pendingAction.type === 'restore' ? 'primary' : 'danger'}
          isLoading={isProcessing}
          onConfirm={handleConfirmAction}
          onCancel={() => !isProcessing && setPendingAction(null)}
        />
      )}
    </section>
  );
}
