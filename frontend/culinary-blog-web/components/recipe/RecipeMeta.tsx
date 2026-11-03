import Link from 'next/link';

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
      ? category.name || category.slug || 'Chưa phân loại'
      : (category || 'Chưa phân loại');

  const authorName =
    typeof author === 'object' && author
      ? author.displayName || author.name || 'Không rõ tác giả'
      : (author || 'Không rõ tác giả');

  return (
    <div className="recipe-card-meta">
      {typeof category === 'object' && category?.slug ? (
        <Link href={`/categories/${encodeURIComponent(category.slug)}`}>{categoryName}</Link>
      ) : (
        <span>{categoryName}</span>
      )}
      <span>Tác giả: {authorName}</span>
      {publishedDate && <time dateTime={publishedAt ?? undefined}>Đăng ngày {publishedDate}</time>}
    </div>
  );
}
