type RecipeMetaProps = {
  category?: string | { id?: string; name?: string; slug?: string } | null;
  author?: string | { id?: string; displayName?: string; name?: string } | null;
  publishedAt?: string | null;
};

function formatDate(value: string | null | undefined): string | null {
  if (!value) return null;

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;

  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium' }).format(date);
}

export default function RecipeMeta({ category, author, publishedAt }: RecipeMetaProps) {
  const publishedDate = formatDate(publishedAt);

  const categoryName =
    typeof category === 'object' && category
      ? category.name || category.slug || 'Ẩm thực'
      : (category || 'Ẩm thực');

  const authorName =
    typeof author === 'object' && author
      ? author.displayName || (author as any).name || 'Đầu bếp'
      : (author || 'Đầu bếp');

  return (
    <div className="recipe-card-meta">
      <span>{categoryName}</span>
      <span>Tác giả: {authorName}</span>
      {publishedDate && <time dateTime={publishedAt ?? undefined}>Đăng ngày {publishedDate}</time>}
    </div>
  );
}
