'use client';

import { useCallback, useEffect, useState } from 'react';
import { apiFetchWithResponse } from '@/lib/api';

type RecipeState = { id: string; slug: string; title: string; status: string; etag: string };

export default function RecipeActions({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<RecipeState | null>(null);
  const [message, setMessage] = useState('');
  const [saving, setSaving] = useState(false);

  const loadRecipe = useCallback(async () => {
    setMessage('');
    try {
      const { data, headers } = await apiFetchWithResponse<Omit<RecipeState, 'etag'>>(`/api/v1/recipes/${slug}`);
      setRecipe({ ...data, etag: headers.get('ETag') ?? '' });
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : 'Không tải được công thức.');
    }
  }, [slug]);

  useEffect(() => {
    void loadRecipe();
  }, [loadRecipe]);

  async function changeStatus(action: 'publish' | 'unpublish' | 'archive' | 'unarchive') {
    if (!recipe || saving) return;
    const confirmations: Partial<Record<typeof action, string>> = {
      unpublish: 'Hủy xuất bản và đưa công thức này về bản nháp?',
      archive: 'Lưu trữ công thức này?',
    };
    if (confirmations[action] && !window.confirm(confirmations[action])) return;

    setSaving(true);
    setMessage('');
    try {
      const response = await apiFetchWithResponse<{ id: string; status: string }>(`/api/v1/recipes/${recipe.id}/${action}`, {
        method: 'POST',
        headers: { 'If-Match': recipe.etag },
      });
      const nextEtag = response.headers.get('ETag') ?? recipe.etag;
      setRecipe((current) => current ? {
        ...current,
        status: response.data.status,
        etag: nextEtag,
      } : current);
      window.dispatchEvent(new CustomEvent('culinary-recipe-etag-change', { detail: { recipeId: recipe.id, etag: nextEtag } }));
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : 'Không thể cập nhật trạng thái.');
    } finally {
      setSaving(false);
    }
  }

  async function softDelete() {
    if (!recipe || saving || !window.confirm(`Xóa mềm công thức "${recipe.title}"? Quản trị viên có thể khôi phục trong thời hạn lưu trữ.`)) return;
    setSaving(true);
    setMessage('');
    try {
      await apiFetchWithResponse(`/api/v1/recipes/${recipe.id}`, {
        method: 'DELETE',
        headers: { 'If-Match': recipe.etag },
      });
      window.location.href = '/recipes';
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : 'Không thể xóa công thức.');
      setSaving(false);
    }
  }

  if (!recipe) return message ? <p className="form-error" role="alert">{message}</p> : null;

  return (
    <div className="form-actions" style={{ marginBottom: 24 }}>
      <a className="outline-button" href={`/recipes/${recipe.slug}/edit`}>Chỉnh sửa</a>
      {recipe.status === 'Published' && (
        <button className="outline-button" type="button" disabled={saving} onClick={() => changeStatus('unpublish')}>Hủy xuất bản</button>
      )}
      {recipe.status === 'Draft' && (
        <button className="primary-button" type="button" disabled={saving} onClick={() => changeStatus('publish')}>Xuất bản</button>
      )}
      {recipe.status === 'Archived' ? (
        <button className="outline-button" type="button" disabled={saving} onClick={() => changeStatus('unarchive')}>Mở lưu trữ</button>
      ) : (
        <button className="outline-button" type="button" disabled={saving} onClick={() => changeStatus('archive')}>Lưu trữ</button>
      )}
      <button className="delete-action" type="button" disabled={saving} onClick={softDelete}>Xóa bài viết</button>
      {message && <span className="form-error" role="alert">{message}</span>}
      {message && <button className="outline-button" type="button" disabled={saving} onClick={() => void loadRecipe()}>Tải lại phiên bản</button>}
    </div>
  );
}
