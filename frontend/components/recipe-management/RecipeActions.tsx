'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type RecipeState = { id: string; slug: string; status: string };

export default function RecipeActions({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<RecipeState | null>(null);
  const [message, setMessage] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    apiFetch<RecipeState>(`/api/v1/recipes/${slug}`)
      .then(setRecipe)
      .catch((reason: Error) => setMessage(reason.message));
  }, [slug]);

  async function changeStatus(action: 'publish' | 'unpublish') {
    if (!recipe) return;
    setSaving(true);
    setMessage('');
    try {
      await apiFetch(`/api/v1/recipes/${recipe.id}/${action}`, { method: 'PATCH' });
      setRecipe((current) => (current ? { ...current, status: action === 'publish' ? 'Published' : 'Draft' } : current));
    } catch (err: unknown) {
      setMessage(err instanceof Error ? err.message : 'Không thể cập nhật trạng thái.');
    } finally {
      setSaving(false);
    }
  }

  if (!recipe) return message ? <p className="form-error">{message}</p> : null;

  return (
    <div className="form-actions" style={{ marginBottom: 24 }}>
      <a className="outline-button" href={`/recipes/${recipe.slug}/edit`}>
        Chỉnh sửa
      </a>
      {recipe.status === 'Published' ? (
        <button className="outline-button" type="button" disabled={saving} onClick={() => changeStatus('unpublish')}>
          Đưa về bản nháp
        </button>
      ) : (
        <button className="primary-button" type="button" disabled={saving} onClick={() => changeStatus('publish')}>
          Xuất bản
        </button>
      )}
      {message && <span className="form-error">{message}</span>}
    </div>
  );
}
