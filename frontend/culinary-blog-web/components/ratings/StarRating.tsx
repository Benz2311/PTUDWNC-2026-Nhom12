'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Star } from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';
import { upsertRating, deleteRating } from '@/lib/api/ratings';
import { useToast } from '@/components/ui/toast';

interface StarRatingProps {
  recipeId: string;
  initialAverage?: number;
  initialCount?: number;
  initialUserRating?: number | null;
}

export default function StarRating({
  recipeId,
  initialAverage = 0,
  initialCount = 0,
  initialUserRating = null,
}: StarRatingProps) {
  const { isAuthenticated } = useAuth();
  const toast = useToast();

  const [average, setAverage] = useState<number>(initialAverage);
  const [count, setCount] = useState<number>(initialCount);
  const [userRating, setUserRating] = useState<number | null>(initialUserRating);
  const [hoverScore, setHoverScore] = useState<number | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  const handleRate = async (score: number) => {
    if (!isAuthenticated) {
      toast?.info?.('Vui lòng đăng nhập để đánh giá công thức.');
      return;
    }

    if (isSubmitting) return;

    setIsSubmitting(true);
    try {
      const summary = await upsertRating(recipeId, score);
      setAverage(summary.averageRating);
      setCount(summary.ratingCount);
      setUserRating(summary.userRating ?? score);
      toast?.success?.(`Bạn đã đánh giá ${score} sao thành công!`);
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { message?: string } }; message?: string };
      const msg = anyErr?.response?.data?.message || anyErr?.message || 'Không thể lưu đánh giá.';
      toast?.error?.(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!isAuthenticated || userRating == null || isSubmitting) return;

    setIsSubmitting(true);
    try {
      const summary = await deleteRating(recipeId);
      setAverage(summary.averageRating);
      setCount(summary.ratingCount);
      setUserRating(null);
      toast?.success?.('Đã xóa đánh giá của bạn.');
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { message?: string } }; message?: string };
      const msg = anyErr?.response?.data?.message || anyErr?.message || 'Không thể xóa đánh giá.';
      toast?.error?.(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const activeStarIndex = hoverScore ?? userRating ?? (count > 0 ? Math.round(average) : 0);

  return (
    <div className="flex flex-col sm:flex-row sm:items-center gap-3 py-3 border-y border-slate-100 my-4">
      {/* Average rating & Stars */}
      <div className="flex items-center gap-2">
        <div
          className="flex items-center"
          onMouseLeave={() => setHoverScore(null)}
          role="group"
          aria-label="Đánh giá sao cho công thức"
        >
          {[1, 2, 3, 4, 5].map((star) => (
            <button
              key={star}
              type="button"
              disabled={isSubmitting}
              onMouseEnter={() => isAuthenticated && setHoverScore(star)}
              onClick={() => handleRate(star)}
              className={`p-1 rounded transition-colors ${
                isAuthenticated ? 'cursor-pointer hover:scale-110' : 'cursor-default'
              }`}
              title={
                isAuthenticated
                  ? `Đánh giá ${star} sao`
                  : 'Đăng nhập để đánh giá'
              }
              aria-label={`${star} sao`}
            >
              <Star
                className={`w-6 h-6 transition-colors ${
                  star <= activeStarIndex
                    ? 'fill-amber-400 text-amber-400'
                    : 'text-slate-300 fill-transparent'
                }`}
              />
            </button>
          ))}
        </div>

        <div className="flex items-baseline gap-1.5 text-sm">
          <span className="font-bold text-slate-900 text-base">
            {count > 0 ? average.toFixed(1) : 'Chưa có đánh giá'}
          </span>
          {count > 0 && (
            <span className="text-slate-500">
              ({count} {count === 1 ? 'lượt đánh giá' : 'lượt đánh giá'})
            </span>
          )}
        </div>
      </div>

      {/* User specific rating info / prompt */}
      <div className="sm:ml-auto text-xs flex items-center gap-2">
        {isAuthenticated ? (
          userRating != null ? (
            <div className="flex items-center gap-2 bg-emerald-50 text-emerald-800 px-2.5 py-1 rounded-full border border-emerald-200">
              <span>Đánh giá của bạn: <strong>{userRating}★</strong></span>
              <button
                type="button"
                onClick={handleDelete}
                disabled={isSubmitting}
                className="text-slate-500 hover:text-red-600 underline font-medium cursor-pointer ml-1"
                title="Xóa đánh giá của bạn"
              >
                Hủy
              </button>
            </div>
          ) : (
            <span className="text-slate-500 italic">
              Bấm vào các ngôi sao ở trên để gửi đánh giá của bạn
            </span>
          )
        ) : (
          <span className="text-slate-500">
            <Link href="/login" className="text-emerald-700 font-semibold hover:underline">
              Đăng nhập
            </Link>{' '}
            để chấm điểm công thức này
          </span>
        )}
      </div>
    </div>
  );
}
