'use client';

import { useCallback, useEffect, useState } from 'react';
import { getRecipeBySlug } from '@/lib/api/recipes';
import type { ApiError } from '@/lib/api';
import type { RecipeDetail as RecipeDetailData } from '@/types/recipe';
import RecipeDetailView from '@/components/recipes/RecipeDetailView';
import RecipeDetailSkeleton from '@/components/recipes/RecipeDetailSkeleton';

type RecipeDetailError = {
  message: string;
  status?: number;
};

export default function RecipeDetail({ slug }: { slug: string }) {
  const [recipe, setRecipe] = useState<RecipeDetailData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<RecipeDetailError | null>(null);

  const loadRecipe = useCallback(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    getRecipeBySlug(slug, true)
      .then(({ data }) => {
        if (!cancelled) {
          setRecipe(data);
          setLoading(false);
        }
      })
      .catch((reason: ApiError) => {
        if (!cancelled) {
          setError({
            message: reason.message || 'Không thể tải công thức.',
            status: reason.status,
          });
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [slug]);

  useEffect(() => {
    const cleanup = loadRecipe();
    return cleanup;
  }, [loadRecipe]);

  if (loading) {
    return <RecipeDetailSkeleton />;
  }

  if (error) {
    if (error.status === 404) {
      return (
        <section className="not-found" role="alert" aria-label="Công thức không tồn tại">
          <span className="not-found-mark" aria-hidden="true">♨</span>
          <h1>404</h1>
          <h2>Công thức không tồn tại</h2>
          <p>
            Không tìm thấy công thức với đường dẫn <code>{slug}</code>. Có thể công thức đã bị xóa hoặc đường dẫn chưa chính xác.
          </p>
          <div style={{ display: 'flex', gap: '12px', marginTop: '18px', flexWrap: 'wrap', justifyContent: 'center' }}>
            <a className="primary-button" href="/recipes">Khám phá công thức khác</a>
            <a className="outline-button" href="/">Quay về trang chủ</a>
          </div>
        </section>
      );
    }

    if (error.status === 403) {
      return (
        <section className="not-found error-state" role="alert" aria-label="Không đủ quyền truy cập">
          <span className="not-found-mark" aria-hidden="true">🔒</span>
          <h2>Không có quyền truy cập</h2>
          <p>
            Công thức này đang ở trạng thái bản nháp (Draft) hoặc lưu trữ (Archived). Chỉ tác giả tạo công thức hoặc Quản trị viên mới được phép xem nội dung này.
          </p>
          <div style={{ display: 'flex', gap: '12px', marginTop: '18px', flexWrap: 'wrap', justifyContent: 'center' }}>
            <a className="primary-button" href="/login">Đăng nhập tài khoản</a>
            <a className="outline-button" href="/recipes">Quay lại danh sách công thức</a>
          </div>
        </section>
      );
    }

    return (
      <div className="empty-state error-state" role="alert">
        <span style={{ fontSize: '36px' }} aria-hidden="true">⚠️</span>
        <strong>Đã xảy ra sự cố khi tải công thức</strong>
        <p style={{ color: 'var(--ink-soft)', margin: '4px 0 16px' }}>{error.message}</p>
        <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', justifyContent: 'center' }}>
          <button type="button" className="primary-button" onClick={loadRecipe}>
            Thử lại
          </button>
          <a className="outline-button" href="/recipes">
            Quay lại danh sách
          </a>
        </div>
      </div>
    );
  }

  if (!recipe) {
    return (
      <div className="empty-state">
        <strong>Không có dữ liệu công thức.</strong>
        <a className="card-action" href="/recipes">Quay lại danh sách</a>
      </div>
    );
  }

  return <RecipeDetailView recipe={recipe} />;
}
