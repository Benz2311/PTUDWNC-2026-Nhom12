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
      ? category.name || category.slug || 'Ẩm thực'
      : (category || 'Ẩm thực');

  const categorySlug =
    typeof category === 'object' && category && category.slug
      ? category.slug
      : encodeURIComponent(String(categoryName).trim().toLowerCase().replace(/\s+/g, '-'));

  const authorName =
    typeof author === 'object' && author
      ? author.displayName || (author as { name?: string }).name || 'Đầu bếp'
      : (author || 'Đầu bếp');

  return (
    <div className="recipe-card-meta flex flex-wrap items-center gap-4 text-sm text-slate-600 my-3">
      <Link
        href={`/categories/${categorySlug}`}
        className="inline-flex items-center gap-1.5 font-medium text-emerald-700 hover:text-emerald-800 hover:underline"
      >
        Danh mục: {categoryName}
      </Link>
      <Link
        href={`/search?q=${encodeURIComponent(String(authorName))}`}
        className="inline-flex items-center gap-1.5 font-medium text-slate-700 hover:text-emerald-700 hover:underline"
      >
        Tác giả: {authorName}
      </Link>
      {publishedDate && (
        <time dateTime={publishedAt ?? undefined} className="text-slate-500">
          Đăng ngày {publishedDate}
        </time>
      )}
    </div>
  );
}
