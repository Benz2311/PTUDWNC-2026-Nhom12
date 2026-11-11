'use client';

import { useEffect, useState } from 'react';
import {
  createRecipeComment,
  getRecipeComments,
  getRecipeFavoriteStatus,
  getRecipeRating,
  setRecipeRating,
  toggleRecipeFavorite,
} from '@/lib/api/recipes';
import type { ApiError } from '@/lib/api';
import type { RecipeComment, RecipeRatingSummary } from '@/types/recipe';

function getInteractionErrorMessage(error: unknown, action: string): string {
  if (error && typeof error === 'object' && 'status' in error) {
    const apiError = error as ApiError;
    if (apiError.status === 401) return 'Vui lòng đăng nhập lại để tiếp tục.';
    if (apiError.status === 403) return 'Bạn không có quyền thực hiện thao tác này.';
    if (apiError.message) return apiError.message;
  }

  return error instanceof Error ? error.message : `Không thể ${action}.`;
}

export default function RecipeInteractionPanel({
  recipeId,
}: {
  recipeId: string;
}) {
  const [favorite, setFavorite] = useState(false);
  const [rating, setRating] = useState<RecipeRatingSummary>({
    averageRating: 0,
    ratingCount: 0,
    myScore: null,
  });
  const [comments, setComments] = useState<RecipeComment[]>([]);
  const [content, setContent] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const [commentsError, setCommentsError] = useState<string | null>(null);
  const [submittingComment, setSubmittingComment] = useState(false);

  useEffect(() => {
    getRecipeRating(recipeId)
      .then(setRating)
      .catch(() => setMessage('Không thể tải thông tin đánh giá.'));

    getRecipeComments(recipeId).then((commentsResult) => {
      setComments(commentsResult.items);
      setCommentsError(null);
    }).catch((error: unknown) => {
      setCommentsError(error instanceof Error ? error.message : 'Không thể tải bình luận.');
    });

    getRecipeFavoriteStatus(recipeId)
      .then((result) => setFavorite(result.isFavorite))
      .catch(() => setFavorite(false));
  }, [recipeId]);

  async function handleFavorite() {
    try {
      const result = await toggleRecipeFavorite(recipeId);
      setFavorite(result.isFavorite);
      setMessage(result.isFavorite ? 'Đã lưu công thức.' : 'Đã bỏ lưu công thức.');
    } catch {
      setMessage('Vui lòng đăng nhập để lưu công thức.');
    }
  }

  async function handleRating(score: number) {
    try {
      setRating(await setRecipeRating(recipeId, score));
      setMessage('Đã lưu đánh giá của bạn.');
    } catch (error: unknown) {
      setMessage(getInteractionErrorMessage(error, 'lưu đánh giá'));
    }
  }

  async function handleComment() {
    const trimmed = content.trim();
    if (!trimmed || submittingComment) return;
    setSubmittingComment(true);
    try {
      const submitted = await createRecipeComment(recipeId, trimmed);
      setContent('');
      setMessage(
        submitted.status === 'Pending'
          ? 'Bình luận đã được gửi và đang chờ kiểm duyệt. Chỉ bạn nhìn thấy bình luận này trong thời gian chờ.'
          : 'Đã gửi bình luận.',
      );
      try {
        const result = await getRecipeComments(recipeId);
        setComments(result.items);
        setCommentsError(null);
      } catch (error: unknown) {
        setCommentsError(error instanceof Error ? error.message : 'Không thể làm mới danh sách bình luận.');
      }
    } catch (error: unknown) {
      setMessage(getInteractionErrorMessage(error, 'gửi bình luận'));
    } finally {
      setSubmittingComment(false);
    }
  }

  return (
    <section className="recipe-interactions" aria-label="Tương tác công thức">
      <div style={{ display: 'flex', gap: '12px', alignItems: 'center', flexWrap: 'wrap' }}>
        <button className="outline-button" type="button" onClick={handleFavorite}>
          {favorite ? '♥ Đã lưu' : '♡ Lưu công thức'}
        </button>
        <span>{rating.averageRating.toFixed(1)} / 5 ({rating.ratingCount} đánh giá)</span>
        {[1, 2, 3, 4, 5].map((score) => (
          <button
            key={score}
            type="button"
            className="outline-button"
            aria-label={`${score} sao`}
            onClick={() => handleRating(score)}
          >
            {score <= (rating.myScore ?? 0) ? '★' : '☆'}
          </button>
        ))}
      </div>

      <div style={{ marginTop: '16px' }}>
        <textarea
          value={content}
          onChange={(event) => setContent(event.target.value)}
          maxLength={2000}
          placeholder="Viết bình luận..."
          style={{ width: '100%', minHeight: 90 }}
        />
        <button
          className="outline-button"
          type="button"
          onClick={handleComment}
          disabled={submittingComment || !content.trim()}
        >
          {submittingComment ? 'Đang gửi...' : 'Gửi bình luận'}
        </button>
      </div>

      {message ? <p role="status">{message}</p> : null}
      {commentsError ? <p role="alert">{commentsError}</p> : null}

      <div style={{ marginTop: '16px' }}>
        {comments.map((comment) => (
          <article key={comment.id} style={{ borderTop: '1px solid #ddd', paddingTop: 12 }}>
            <strong>{comment.authorName || 'Người dùng'}</strong>
            {comment.status === 'Pending' ? <span> · Chờ kiểm duyệt</span> : null}
            <p>{comment.content}</p>
            {comment.replies.map((reply) => (
              <blockquote key={reply.id}>
                <strong>{reply.authorName || 'Người dùng'}</strong>
                {reply.status === 'Pending' ? <span> · Chờ kiểm duyệt</span> : null}
                <p>{reply.content}</p>
              </blockquote>
            ))}
          </article>
        ))}
      </div>
    </section>
  );
}
