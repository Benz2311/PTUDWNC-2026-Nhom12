'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type DeletedRecipe = {
  id: string;
  title: string;
  slug: string;
  status: string;
  deletedAt: string;
};

export default function RecipeTrash() {
  const [recipes, setRecipes] = useState<DeletedRecipe[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState('');

  async function loadTrash() {
    setError('');
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

  async function restore(id: string) {
    setBusyId(id);
    setError('');
    try {
      await apiFetch(`/api/v1/admin/recipes/${id}/restore`, { method: 'POST' });
      setRecipes((current) => current.filter((recipe) => recipe.id !== id));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể khôi phục công thức.');
    } finally {
      setBusyId('');
    }
  }

  async function purge(recipe: DeletedRecipe) {
    if (!window.confirm(`Xóa vĩnh viễn "${recipe.title}" cùng toàn bộ ảnh và dữ liệu liên quan? Thao tác này không thể hoàn tác.`)) return;
    setBusyId(recipe.id);
    setError('');
    try {
      await apiFetch(`/api/v1/admin/recipes/${recipe.id}/purge`, { method: 'DELETE' });
      setRecipes((current) => current.filter((item) => item.id !== recipe.id));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không thể xóa vĩnh viễn công thức.');
    } finally {
      setBusyId('');
    }
  }

  return (
    <section className="flow-panel" style={{ marginTop: 28 }}>
      <div className="form-actions">
        <h2 style={{ margin: 0 }}>Công thức đã xóa</h2>
        <button className="outline-button" type="button" onClick={() => void loadTrash()} disabled={loading}>Làm mới</button>
      </div>
      {error && <p className="form-error" role="alert">{error}</p>}
      {loading ? <p>Đang tải thùng rác...</p> : recipes.length === 0 ? (
        <p>Thùng rác hiện không có công thức nào.</p>
      ) : (
        <div className="recipe-admin-grid">
          {recipes.map((recipe) => (
            <article className="recipe-admin-card" key={recipe.id}>
              <div className="recipe-admin-card-top">
                <span className="status status-archived">{recipe.status}</span>
                <span>Đã xóa {new Date(recipe.deletedAt).toLocaleString('vi-VN')}</span>
              </div>
              <h3>{recipe.title}</h3>
              <div className="card-actions">
                <button className="outline-button" type="button" disabled={busyId === recipe.id} onClick={() => void restore(recipe.id)}>Khôi phục</button>
                <button className="delete-action" type="button" disabled={busyId === recipe.id} onClick={() => void purge(recipe)}>Xóa vĩnh viễn</button>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
