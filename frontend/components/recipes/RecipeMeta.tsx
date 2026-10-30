type RecipeMetaProps = {
  category?: string | null;
  author?: string | null;
  publishedAt?: string | null;
};

function formatDate(value?: string | null): string | null {
  if (!value) return null;

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;

  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium' }).format(date);
}

export default function RecipeMeta({ category, author, publishedAt }: RecipeMetaProps) {
  const publishedDate = formatDate(publishedAt);

  return (
    <div className="recipe-card-meta">
      {category && <span>{category}</span>}
      {author && <span>Tác giả: {author}</span>}
      {publishedDate && <time dateTime={publishedAt ?? undefined}>Đăng ngày {publishedDate}</time>}
    </div>
  );
}
