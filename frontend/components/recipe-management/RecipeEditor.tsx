'use client';

import { useCallback, useEffect, useState } from 'react';
import { apiJson } from '@/lib/api';
import {
  changeRecipeStatus,
  getRecipeBySlug,
  getRecipeEtag,
  getRecipeEtagFromResponse,
} from '@/lib/api/recipes';
import RecipeForm, { InitialRecipe } from './RecipeForm';
import RecipeVersionHistory from './RecipeVersionHistory';

type CurrentUser = {
  id: string;
  userName?: string;
  email?: string;
  roles?: string[];
  emailConfirmed?: boolean;
};

export default function RecipeEditor({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<InitialRecipe | null>(null);
  const [etag, setEtag] = useState('');
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [unpublishing, setUnpublishing] = useState(false);
  const [revisionRefreshKey, setRevisionRefreshKey] = useState(0);

  const loadRecipe = useCallback(async (): Promise<boolean> => {
    setLoading(true);
    setError('');
    try {
      const { data, headers } = await getRecipeBySlug(slug);
      setRecipe(data);

      let currentEtag = getRecipeEtagFromResponse(headers, data.version);
      if (!currentEtag && data.id) {
        currentEtag = await getRecipeEtag(data.id, slug);
      }
      if (!currentEtag) throw new Error('Không nhận được phiên bản (ETag) của công thức.');
      setEtag(currentEtag);
      return true;
    } catch (reason: unknown) {
      setError(reason instanceof Error ? reason.message : 'Không thể tải công thức.');
      return false;
    } finally {
      setLoading(false);
    }
  }, [slug]);

  useEffect(() => {
    void loadRecipe();
  }, [loadRecipe]);

  useEffect(() => {
    apiJson<CurrentUser>('/api/v1/auth/me')
      .then(setCurrentUser)
      .catch(() => {
        // User not logged in
      });
  }, []);

  if (loading && !recipe) {
    return (
      <div className="loading-state" role="status">
        <span className="spinner" aria-hidden="true" /> Đang tải dữ liệu công thức...
      </div>
    );
  }

  if (error && !recipe) {
    return (
      <div className="empty-state error-state" role="alert">
        <strong>{error}</strong>
        <div style={{ display: 'flex', gap: 12, marginTop: 16 }}>
          <button className="primary-button" type="button" onClick={() => void loadRecipe()}>
            Thử lại
          </button>
          <a className="outline-button" href={`/recipes/${slug}`}>
            Quay lại chi tiết
          </a>
        </div>
      </div>
    );
  }

  if (!recipe) {
    return <div className="loading-state">Không tìm thấy công thức.</div>;
  }

  // 1. Ownership check: Chỉ được chỉnh sửa công thức của chính bản thân
  const isOwner =
    !currentUser ||
    !recipe.authorId ||
    currentUser.id === recipe.authorId ||
    currentUser.roles?.includes('Admin');

  if (currentUser && recipe.authorId && !isOwner) {
    return (
      <div className="empty-state error-state" role="alert" style={{ maxWidth: 640, margin: '40px auto', textAlign: 'center' }}>
        <div style={{ fontSize: 48, marginBottom: 16 }}>🚫</div>
        <h2>Không có quyền chỉnh sửa</h2>
        <p style={{ color: '#64748b', margin: '12px 0 24px' }}>
          Người dùng chỉ được phép chỉnh sửa công thức do chính bản thân tạo ra.
        </p>
        <a className="primary-button" href={`/recipes/${slug}`}>
          Xem chi tiết công thức
        </a>
      </div>
    );
  }

  // 2. State check: Chỉnh sửa chỉ được phép khi công thức ở trong Kho lưu trữ (Draft/Archived)
  if (recipe.status === 'Published') {
    return (
      <div className="empty-state" role="alert" style={{ maxWidth: 640, margin: '40px auto', textAlign: 'center' }}>
        <div style={{ fontSize: 48, marginBottom: 16 }}>📦</div>
        <h2>Công thức đang ở trạng thái Đã xuất bản</h2>
        <p style={{ color: '#64748b', margin: '12px 0 24px' }}>
          Chỉnh sửa chỉ được phép khi công thức ở trong <strong>Kho lưu trữ</strong>.
          Vui lòng hủy xuất bản công thức về Kho lưu trữ trước khi chỉnh sửa.
        </p>
        <div style={{ display: 'flex', gap: 12, justifyContent: 'center' }}>
          <button
            type="button"
            className="primary-button"
            disabled={unpublishing}
            onClick={async () => {
              setUnpublishing(true);
              setError('');
              try {
                const activeEtag = etag || (await getRecipeEtag(recipe.id, slug));
                await changeRecipeStatus(recipe.id, 'unpublish', activeEtag);
                await loadRecipe();
              } catch (err: unknown) {
                setError(err instanceof Error ? err.message : 'Không thể chuyển công thức về Kho lưu trữ.');
              } finally {
                setUnpublishing(false);
              }
            }}
          >
            {unpublishing ? '⏳ Đang chuyển về Kho lưu trữ...' : '↩ Hủy xuất bản về Kho lưu trữ để sửa'}
          </button>
          <a className="outline-button" href={`/recipes/${slug}`}>
            Quay lại chi tiết
          </a>
        </div>
      </div>
    );
  }

  return (
    <>
      <RecipeForm
        key={etag}
        recipeId={recipe.id}
        initialRecipe={recipe}
        initialEtag={etag}
        onReload={loadRecipe}
        onSuccess={() => setRevisionRefreshKey((prev) => prev + 1)}
      />
      <RecipeVersionHistory recipeId={recipe.id} refreshKey={revisionRefreshKey} />
    </>
  );
}
