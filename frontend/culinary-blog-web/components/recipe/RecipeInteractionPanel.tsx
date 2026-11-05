'use client';

import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  addRecipeComment,
  getRecipeComments,
  getRecipeFavoriteStatus,
  getRecipeRatings,
  toggleRecipeFavorite,
  upsertRecipeRating,
} from '@/lib/api/recipes';
import type { RecipeComment } from '@/types/recipe';

export function RecipeInteractionPanel({ recipeId }: { recipeId: string }) {
  const [isFavorite, setIsFavorite] = useState(false);
  const [averageRating, setAverageRating] = useState(0);
  const [ratingCount, setRatingCount] = useState(0);
  const [myScore, setMyScore] = useState<number | null>(null);
  const [comments, setComments] = useState<RecipeComment[]>([]);
  const [comment, setComment] = useState('');
  const [parentCommentId, setParentCommentId] = useState<string | null>(null);
  const [message, setMessage] = useState('');

  const loadPublicData = useCallback(async () => {
    try {
      const rating = await getRecipeRatings(recipeId);
      setAverageRating(rating.averageRating);
      setRatingCount(rating.ratingCount);
      setMyScore(rating.myScore);
    } catch {
      // Keep the interaction panel usable if summary loading fails.
    }

    try {
      const result = await getRecipeComments(recipeId);
      setComments(result.items);
    } catch {
      setComments([]);
    }

    try {
      const favorite = await getRecipeFavoriteStatus(recipeId);
      setIsFavorite(favorite.isFavorite);
    } catch {
      setIsFavorite(false);
    }
  }, [recipeId]);

  useEffect(() => {
    void loadPublicData();
  }, [loadPublicData]);

  const handleFavorite = async () => {
    try {
      const result = await toggleRecipeFavorite(recipeId);
      setIsFavorite(result.isFavorite);
      setMessage(result.isFavorite ? 'Đã lưu công thức.' : 'Đã bỏ lưu công thức.');
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Không thể cập nhật danh sách yêu thích.');
    }
  };

  const handleRating = async (score: number) => {
    try {
      const rating = await upsertRecipeRating(recipeId, score);
      setAverageRating(rating.averageRating);
      setRatingCount(rating.ratingCount);
      setMyScore(rating.myScore);
      setMessage(`Đã đánh giá ${score}/5.`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Không thể gửi đánh giá.');
    }
  };

  const handleComment = async (event: FormEvent) => {
    event.preventDefault();
    if (!comment.trim()) {
      setMessage('Vui lòng nhập nội dung bình luận.');
      return;
    }

    try {
      await addRecipeComment(recipeId, comment.trim(), parentCommentId ?? undefined);
      setComment('');
      setParentCommentId(null);
      setMessage('Đã gửi bình luận.');
      const result = await getRecipeComments(recipeId);
      setComments(result.items);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Không thể gửi bình luận.');
    }
  };

  return (
    <section className="recipe-interaction-panel mt-10 rounded-2xl border border-slate-200 bg-white p-5">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <button
          type="button"
          className={`rounded-xl px-4 py-2 text-sm font-semibold ${isFavorite ? 'bg-rose-100 text-rose-700' : 'bg-slate-100 text-slate-700'}`}
          onClick={handleFavorite}
        >
          {isFavorite ? '♥ Đã lưu' : '♡ Lưu công thức'}
        </button>

        <div className="flex items-center gap-2" aria-label="Đánh giá công thức">
          {[1, 2, 3, 4, 5].map((score) => (
            <button
              type="button"
              key={score}
              className={`text-xl ${myScore !== null && score <= myScore ? 'text-amber-500' : 'text-slate-300'}`}
              onClick={() => handleRating(score)}
              aria-label={`Đánh giá ${score} sao`}
            >
              ★
            </button>
          ))}
          <span className="ml-2 text-sm text-slate-600">
            {averageRating.toFixed(1)} ({ratingCount})
          </span>
        </div>
      </div>

      <div className="mt-8">
        <h2 className="text-xl font-bold text-slate-900">Bình luận</h2>
        <form className="mt-4" onSubmit={handleComment}>
          {parentCommentId && (
            <div className="mb-2 flex items-center justify-between rounded-lg bg-slate-100 px-3 py-2 text-sm">
              <span>Đang trả lời một bình luận</span>
              <button type="button" className="font-semibold" onClick={() => setParentCommentId(null)}>Hủy</button>
            </div>
          )}
          <textarea
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            className="min-h-24 w-full rounded-xl border border-slate-300 px-3 py-2"
            maxLength={2000}
            placeholder="Viết bình luận về công thức..."
          />
          <button type="submit" className="mt-3 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white">
            Gửi bình luận
          </button>
        </form>

        {message && <p className="mt-3 text-sm text-slate-600" role="status">{message}</p>}

        <div className="mt-6 space-y-4">
          {comments.length === 0 ? (
            <p className="text-sm text-slate-500">Chưa có bình luận.</p>
          ) : comments.map((item) => (
            <article className="rounded-xl border border-slate-200 p-4" key={item.id}>
              <div className="flex items-center justify-between gap-3">
                <strong className="text-sm text-slate-900">{item.author}</strong>
                <button type="button" className="text-xs text-emerald-700" onClick={() => setParentCommentId(item.id)}>
                  Trả lời
                </button>
              </div>
              <p className="mt-2 text-sm text-slate-700">{item.content}</p>
              {item.replies.map((reply) => (
                <div className="ml-6 mt-3 border-l-2 border-slate-200 pl-4" key={reply.id}>
                  <strong className="text-sm text-slate-900">{reply.author}</strong>
                  <p className="mt-1 text-sm text-slate-700">{reply.content}</p>
                </div>
              ))}
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
