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
import type { RecipeComment, RecipeRatingSummary } from '@/types/recipe';

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

  useEffect(() => {
    Promise.all([
      getRecipeRating(recipeId),
      getRecipeComments(recipeId),
    ]).then(([ratingResult, commentsResult]) => {
      setRating(ratingResult);
      setComments(commentsResult.items);
    }).catch(() => setMessage('Không thể tải dữ liệu tương tác.'));

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
    } catch {
      setMessage('Vui lòng đăng nhập để đánh giá.');
    }
  }

  async function handleComment() {
    const trimmed = content.trim();
    if (!trimmed) return;
    try {
      await createRecipeComment(recipeId, trimmed);
      setContent('');
      const result = await getRecipeComments(recipeId);
      setComments(result.items);
      setMessage('Đã đăng bình luận.');
    } catch {
      setMessage('Vui lòng đăng nhập để bình luận.');
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
        <button className="outline-button" type="button" onClick={handleComment}>
          Gửi bình luận
        </button>
      </div>

      {message ? <p role="status">{message}</p> : null}

      <div style={{ marginTop: '16px' }}>
        {comments.map((comment) => (
          <article key={comment.id} style={{ borderTop: '1px solid #ddd', paddingTop: 12 }}>
            <strong>{comment.authorName}</strong>
            <p>{comment.content}</p>
            {comment.replies.map((reply) => (
              <blockquote key={reply.id}>
                <strong>{reply.authorName}</strong>
                <p>{reply.content}</p>
              </blockquote>
            ))}
          </article>
        ))}
      </div>
    </section>
  );
}
