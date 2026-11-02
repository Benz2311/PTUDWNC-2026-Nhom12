'use client';

import { useCallback, useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { getRecipeBySlug } from '@/lib/api/recipes';
import type { RecipeDetail } from '@/types/recipe';
import RecipeDetailSkeleton from '@/components/recipes/RecipeDetailSkeleton';
import { RecipeDetailView } from '@/components/recipes/RecipeDetailView';

export default function RecipeDetailPage() {
  const router = useRouter();
  const params = useParams<{ slug: string }>();
  const slug = typeof params?.slug === 'string' ? decodeURIComponent(params.slug) : '';

  const [recipe, setRecipe] = useState<RecipeDetail | null>(null);
  const [status, setStatus] = useState<'loading' | 'ready' | 'not-found' | 'forbidden' | 'error'>('loading');
  const [errorMessage, setErrorMessage] = useState<string>('');

  const loadRecipe = useCallback(async () => {
    if (!slug) {
      setStatus('not-found');
      return;
    }

    // Nếu slug là 'create', chuyển hướng an toàn sang trang tạo công thức riêng
    if (slug === 'create') {
      router.replace('/recipes/create');
      return;
    }

    setStatus('loading');
    setErrorMessage('');

    try {
      const data = await getRecipeBySlug(slug);
      if (!data) {
        setStatus('not-found');
        return;
      }
      setRecipe(data);
      setStatus('ready');
    } catch (err: unknown) {
      const anyErr = err as { status?: number; statusCode?: number; response?: { status?: number }; message?: string };
      const statusCode = anyErr?.status ?? anyErr?.statusCode ?? anyErr?.response?.status;
      if (statusCode === 404) {
        setStatus('not-found');
      } else if (statusCode === 401 || statusCode === 403) {
        setStatus('forbidden');
      } else {
        setErrorMessage(anyErr?.message || 'Không thể tải thông tin công thức vào lúc này.');
        setStatus('error');
      }
    }
  }, [slug, router]);

  useEffect(() => {
    if (slug === 'create') {
      router.replace('/recipes/create');
      return;
    }
    loadRecipe();
  }, [slug, loadRecipe, router]);

  if (status === 'loading') {
    return <RecipeDetailSkeleton />;
  }

  if (status === 'not-found') {
    return (
      <section className="mx-auto max-w-2xl px-4 py-16 text-center" role="status">
        <span className="inline-block rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800 mb-3">
          404 Not Found
        </span>
        <h1 className="text-2xl font-bold text-slate-900 mb-2">Không tìm thấy công thức</h1>
        <p className="text-slate-600 mb-6">
          Công thức với đường dẫn <strong>{slug}</strong> không tồn tại hoặc đã bị gỡ khỏi hệ thống.
        </p>
        <div className="flex justify-center gap-3">
          <Link
            href="/"
            className="rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700 shadow-xs"
          >
            Về trang chủ
          </Link>
          <Link
            href="/search"
            className="rounded-xl border border-slate-300 bg-white px-5 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            Tìm kiếm công thức
          </Link>
        </div>
      </section>
    );
  }

  if (status === 'forbidden') {
    return (
      <section className="mx-auto max-w-2xl px-4 py-16 text-center" role="alert">
        <span className="inline-block rounded-full bg-red-100 px-3 py-1 text-xs font-semibold text-red-800 mb-3">
          Hạn chế truy cập
        </span>
        <h1 className="text-2xl font-bold text-slate-900 mb-2">Công thức chưa được công khai</h1>
        <p className="text-slate-600 mb-6">
          Công thức ở trạng thái <strong>Draft</strong> hoặc <strong>Archived</strong> chỉ hiển thị cho tác giả sở hữu hoặc quản trị viên.
        </p>
        <div className="flex justify-center gap-3">
          <Link
            href="/login"
            className="rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700"
          >
            Đăng nhập để xem
          </Link>
          <Link
            href="/"
            className="rounded-xl border border-slate-300 bg-white px-5 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            Về trang chủ
          </Link>
        </div>
      </section>
    );
  }

  if (status === 'error' || !recipe) {
    return (
      <section className="mx-auto max-w-2xl px-4 py-16 text-center" role="alert">
        <span className="inline-block rounded-full bg-red-100 px-3 py-1 text-xs font-semibold text-red-800 mb-3">
          Lỗi tải trang
        </span>
        <h1 className="text-2xl font-bold text-slate-900 mb-2">Đã xảy ra sự cố</h1>
        <p className="text-slate-600 mb-6">{errorMessage || 'Không thể hiển thị công thức này.'}</p>
        <button
          type="button"
          onClick={loadRecipe}
          className="rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700 cursor-pointer"
        >
          Thử lại
        </button>
      </section>
    );
  }

  return (
    <article className="mx-auto max-w-5xl px-4 py-8">
      <RecipeDetailView recipe={recipe} />
    </article>
  );
}
