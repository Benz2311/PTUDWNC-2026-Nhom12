"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { phaseFourApi, type FeedPage as FeedPageData } from "@/lib/api/phase-four";
import PhaseFourNavigation from "@/components/phase-four/PhaseFourNavigation";

const activityLabels = {
  CreatedRecipe: "đã đăng công thức mới",
  UpdatedRecipe: "đã cập nhật công thức",
  RatedRecipe: "đã đánh giá công thức",
  CommentedRecipe: "đã bình luận về công thức",
} as const;

export default function FeedPage() {
  const [feed, setFeed] = useState<FeedPageData | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError("");
    phaseFourApi.getFeed(page)
      .then((result) => {
        if (active) setFeed(result);
      })
      .catch(() => {
        if (active) setError("Không thể tải bảng tin. Vui lòng thử lại.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [page]);

  const totalPages = Math.max(1, Math.ceil((feed?.totalCount ?? 0) / (feed?.pageSize ?? 20)));

  return (
    <main className="mx-auto max-w-4xl px-4 py-10">
      <PhaseFourNavigation />
      <header className="mb-8">
        <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">Cộng đồng</p>
        <h1 className="mt-1 text-3xl font-bold text-slate-950">Bảng tin của tôi</h1>
        <p className="mt-2 text-slate-600">Cập nhật mới nhất từ những tác giả bạn đang theo dõi.</p>
      </header>

      {error && <p role="alert" className="rounded-lg bg-red-50 p-4 text-red-700">{error}</p>}
      {loading && <p role="status" className="py-8 text-slate-600">Đang tải bảng tin…</p>}
      {!loading && !error && feed?.items.length === 0 && (
        <section className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center">
          <h2 className="text-lg font-semibold text-slate-900">Bảng tin đang trống</h2>
          <p className="mt-2 text-slate-600">Theo dõi tác giả để xem công thức và hoạt động mới tại đây.</p>
          <Link href="/recipes" className="mt-4 inline-flex rounded-lg bg-emerald-700 px-4 py-2 font-semibold text-white">
            Khám phá công thức
          </Link>
        </section>
      )}

      {!loading && feed && feed.items.length > 0 && (
        <>
          <ol className="space-y-4">
            {feed.items.map((item) => (
              <li key={item.id} className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
                <article className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center">
                  <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-emerald-100 font-bold text-emerald-800" aria-hidden="true">
                    {item.userName.slice(0, 1).toLocaleUpperCase("vi-VN")}
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm text-slate-700">
                      <strong>{item.userName}</strong> {activityLabels[item.activityType]}
                    </p>
                    <Link href={`/recipes/${encodeURIComponent(item.recipeSlug)}`} className="mt-1 block text-lg font-bold text-slate-950 hover:text-emerald-700">
                      {item.recipeTitle}
                    </Link>
                    <time className="mt-1 block text-xs text-slate-500" dateTime={item.createdAt}>
                      {new Date(item.createdAt).toLocaleString("vi-VN")}
                    </time>
                  </div>
                  {item.imageUrl && (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={item.imageUrl} alt="" className="h-24 w-full rounded-xl object-cover sm:w-36" loading="lazy" />
                  )}
                </article>
              </li>
            ))}
          </ol>
          <nav aria-label="Phân trang bảng tin" className="mt-6 flex items-center justify-between">
            <button type="button" disabled={page <= 1} onClick={() => setPage((value) => value - 1)} className="rounded-lg border px-4 py-2 text-sm disabled:opacity-50">
              Trang trước
            </button>
            <span className="text-sm text-slate-600">Trang {page} / {totalPages}</span>
            <button type="button" disabled={page >= totalPages} onClick={() => setPage((value) => value + 1)} className="rounded-lg border px-4 py-2 text-sm disabled:opacity-50">
              Trang sau
            </button>
          </nav>
        </>
      )}
    </main>
  );
}
