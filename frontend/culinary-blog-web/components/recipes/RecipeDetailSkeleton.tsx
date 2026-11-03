export default function RecipeDetailSkeleton() {
  return (
    <div className="recipe-detail recipe-detail-skeleton mx-auto max-w-6xl px-4 py-8" aria-busy="true" aria-label="Đang tải công thức">
      <div className="skeleton-line skeleton-breadcrumb h-4 w-48 bg-slate-200 rounded mb-4 animate-pulse" />
      <div className="recipe-detail-header mb-6">
        <div className="recipe-detail-badges flex gap-2 mb-3">
          <span className="skeleton-pill h-6 w-24 bg-slate-200 rounded-full animate-pulse" />
          <span className="skeleton-pill h-6 w-20 bg-slate-200 rounded-full animate-pulse" />
        </div>
        <div className="skeleton-line skeleton-title h-10 w-3/4 bg-slate-200 rounded mb-3 animate-pulse" />
        <div className="skeleton-line skeleton-lead h-5 w-2/3 bg-slate-200 rounded mb-4 animate-pulse" />
        <div className="skeleton-meta-grid grid grid-cols-3 gap-4 max-w-md">
          <span className="skeleton-box h-16 bg-slate-200 rounded-xl animate-pulse" />
          <span className="skeleton-box h-16 bg-slate-200 rounded-xl animate-pulse" />
          <span className="skeleton-box h-16 bg-slate-200 rounded-xl animate-pulse" />
        </div>
      </div>

      <div className="skeleton-cover h-80 w-full bg-slate-200 rounded-2xl mb-8 animate-pulse" />

      <div className="detail-columns recipe-detail-columns grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-4">
          <div className="skeleton-section-title h-7 w-40 bg-slate-200 rounded animate-pulse" />
          <div className="skeleton-line h-4 w-full bg-slate-200 rounded animate-pulse" />
          <div className="skeleton-line h-4 w-5/6 bg-slate-200 rounded animate-pulse" />
          <div className="skeleton-section-title h-7 w-48 bg-slate-200 rounded mt-6 animate-pulse" />
          <div className="skeleton-box h-32 w-full bg-slate-200 rounded-xl animate-pulse" />
        </div>
        <div className="skeleton-nutrition h-64 w-full bg-slate-200 rounded-2xl animate-pulse" />
      </div>
    </div>
  );
}
