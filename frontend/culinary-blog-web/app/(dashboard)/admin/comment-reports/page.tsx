'use client';

import { useCallback, useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
  Flag,
  ShieldAlert,
  CheckCircle2,
  EyeOff,
  Filter,
  Loader2,
  ExternalLink,
  ChevronLeft,
  ChevronRight,
  AlertTriangle,
} from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';
import { getRolesFromToken } from '@/lib/jwt';
import { getAdminCommentReports, resolveCommentReport } from '@/lib/api/reports';
import type { CommentReportItem } from '@/types/report';
import { useToast } from '@/components/ui/toast';

export default function AdminCommentReportsPage() {
  const router = useRouter();
  const { accessToken, isAuthenticated, isLoading: authLoading } = useAuth();
  const toast = useToast();

  const isAdmin =
    isAuthenticated &&
    getRolesFromToken(accessToken).some((role) => role.toLowerCase() === 'admin');

  const [reports, setReports] = useState<CommentReportItem[]>([]);
  const [statusFilter, setStatusFilter] = useState<string>('Pending');
  const [page, setPage] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [hasNextPage, setHasNextPage] = useState<boolean>(false);
  const [hasPreviousPage, setHasPreviousPage] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Selected report for resolution dialog
  const [activeReport, setActiveReport] = useState<CommentReportItem | null>(null);
  const [resolveAction, setResolveAction] = useState<'Dismiss' | 'HideComment'>('Dismiss');
  const [resolutionNotes, setResolutionNotes] = useState<string>('');
  const [isResolving, setIsResolving] = useState<boolean>(false);

  const fetchReports = useCallback(
    async (pageToLoad: number, filterToApply: string) => {
      setIsLoading(true);
      setError(null);
      try {
        const result = await getAdminCommentReports(
          filterToApply === 'all' ? undefined : filterToApply,
          pageToLoad,
          10
        );
        setReports(result.items);
        setPage(result.page);
        setTotalPages(result.totalPages);
        setTotalCount(result.totalCount);
        setHasNextPage(result.hasNextPage);
        setHasPreviousPage(result.hasPreviousPage);
      } catch (err: unknown) {
        const anyErr = err as { message?: string };
        setError(anyErr?.message || 'Không thể tải danh sách báo cáo vi phạm.');
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  useEffect(() => {
    if (!authLoading && !isAdmin) {
      router.replace('/');
      return;
    }
    if (isAdmin) {
      fetchReports(page, statusFilter);
    }
  }, [authLoading, isAdmin, page, statusFilter, fetchReports, router]);

  const handleOpenResolve = (report: CommentReportItem, action: 'Dismiss' | 'HideComment') => {
    setActiveReport(report);
    setResolveAction(action);
    setResolutionNotes('');
  };

  const handleConfirmResolve = async () => {
    if (!activeReport) return;

    setIsResolving(true);
    try {
      await resolveCommentReport(
        activeReport.id,
        resolveAction,
        resolutionNotes.trim() || undefined
      );

      toast?.success?.(
        resolveAction === 'HideComment'
          ? 'Đã ẩn bình luận vi phạm và giải quyết báo cáo!'
          : 'Đã giữ lại bình luận và bác bỏ báo cáo!'
      );

      setActiveReport(null);
      fetchReports(page, statusFilter);
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { message?: string } }; message?: string };
      const msg =
        anyErr?.response?.data?.message ||
        anyErr?.message ||
        'Không thể xử lý báo cáo vào lúc này.';
      toast?.error?.(msg);
    } finally {
      setIsResolving(false);
    }
  };

  const formatDate = (dateStr: string) => {
    try {
      const date = new Date(dateStr);
      return new Intl.DateTimeFormat('vi-VN', {
        dateStyle: 'short',
        timeStyle: 'short',
      }).format(date);
    } catch {
      return dateStr;
    }
  };

  if (authLoading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-emerald-700" />
      </div>
    );
  }

  if (!isAdmin) {
    return null;
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      {/* Header */}
      <div className="mb-6 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200 pb-5">
        <div>
          <div className="flex items-center gap-2 text-rose-600 mb-1">
            <ShieldAlert className="w-5 h-5" />
            <span className="text-xs font-bold uppercase tracking-wider">
              Quản trị hệ thống
            </span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-bold text-slate-900">
            Quản lý báo cáo bình luận
          </h1>
          <p className="text-sm text-slate-600 mt-1">
            Xem xét các bình luận bị người dùng báo cáo vi phạm, giữ lại hoặc ẩn bình luận.
          </p>
        </div>

        {/* Filter Tabs */}
        <div className="flex items-center gap-1.5 rounded-xl bg-slate-100 p-1 self-start sm:self-auto">
          {[
            { id: 'Pending', label: 'Chờ xử lý' },
            { id: 'ActionTaken', label: 'Đã ẩn' },
            { id: 'Dismissed', label: 'Đã giữ lại' },
            { id: 'all', label: 'Tất cả' },
          ].map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => {
                setStatusFilter(tab.id);
                setPage(1);
              }}
              className={`rounded-lg px-3 py-1.5 text-xs font-semibold transition cursor-pointer ${
                statusFilter === tab.id
                  ? 'bg-white text-slate-900 shadow-2xs'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Error state */}
      {error && (
        <div className="mb-6 rounded-2xl bg-red-50 p-4 border border-red-200 text-sm text-red-700 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-5 h-5 text-red-500 shrink-0" />
            <span>{error}</span>
          </div>
          <button
            type="button"
            onClick={() => fetchReports(page, statusFilter)}
            className="rounded-lg bg-white px-3 py-1 text-xs font-semibold text-red-700 hover:bg-red-50 border border-red-200 cursor-pointer"
          >
            Thử lại
          </button>
        </div>
      )}

      {/* Loading state */}
      {isLoading ? (
        <div className="flex h-64 items-center justify-center">
          <Loader2 className="w-8 h-8 animate-spin text-emerald-700" />
        </div>
      ) : reports.length === 0 ? (
        /* Empty state */
        <div className="rounded-2xl border border-dashed border-slate-200 py-16 text-center">
          <CheckCircle2 className="w-12 h-12 text-emerald-500 mx-auto mb-3" />
          <h3 className="text-base font-semibold text-slate-800 mb-1">
            Không có báo cáo nào
          </h3>
          <p className="text-sm text-slate-500">
            Hiện tại không có bình luận nào cần xem xét trong danh mục này.
          </p>
        </div>
      ) : (
        /* Table of Reports */
        <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-2xs">
          <table className="w-full text-left text-sm text-slate-600">
            <thead className="bg-slate-50 text-xs font-semibold uppercase text-slate-500 border-b border-slate-200">
              <tr>
                <th className="px-4 py-3.5">Thời gian</th>
                <th className="px-4 py-3.5">Người báo cáo</th>
                <th className="px-4 py-3.5">Bình luận & Công thức</th>
                <th className="px-4 py-3.5">Lý do báo cáo</th>
                <th className="px-4 py-3.5">Trạng thái</th>
                <th className="px-4 py-3.5 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {reports.map((report) => (
                <tr key={report.id} className="hover:bg-slate-50/50 transition">
                  <td className="px-4 py-3 text-xs whitespace-nowrap text-slate-500">
                    {formatDate(report.createdAt)}
                  </td>

                  <td className="px-4 py-3 text-xs whitespace-nowrap">
                    <span className="font-medium text-slate-900">
                      {report.reporterName}
                    </span>
                  </td>

                  <td className="px-4 py-3 max-w-md">
                    <div className="space-y-1">
                      <p
                        className={`text-sm ${
                          report.isCommentHidden
                            ? 'text-slate-400 italic'
                            : 'text-slate-800 font-medium'
                        }`}
                      >
                        &ldquo;{report.commentContent}&rdquo;
                      </p>
                      <div className="flex items-center gap-2 text-xs text-slate-500">
                        <span>Tác giả: <strong>{report.commentAuthorName}</strong></span>
                        <span>•</span>
                        {report.recipeSlug ? (
                          <Link
                            href={`/recipes/${encodeURIComponent(report.recipeSlug)}`}
                            target="_blank"
                            className="inline-flex items-center gap-0.5 text-emerald-700 hover:underline"
                          >
                            <span>{report.recipeTitle}</span>
                            <ExternalLink className="w-3 h-3" />
                          </Link>
                        ) : (
                          <span>{report.recipeTitle}</span>
                        )}
                      </div>
                    </div>
                  </td>

                  <td className="px-4 py-3">
                    <span className="inline-block rounded-md bg-rose-50 px-2 py-1 text-xs font-semibold text-rose-700 border border-rose-200 mb-1">
                      {report.reason}
                    </span>
                    {report.details && (
                      <p className="text-xs text-slate-500 italic mt-0.5">
                        {report.details}
                      </p>
                    )}
                  </td>

                  <td className="px-4 py-3 whitespace-nowrap">
                    {report.status === 'Pending' && (
                      <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-medium text-amber-800">
                        Chờ xử lý
                      </span>
                    )}
                    {report.status === 'ActionTaken' && (
                      <span className="inline-flex items-center gap-1 rounded-full bg-rose-100 px-2.5 py-0.5 text-xs font-medium text-rose-800">
                        Đã ẩn bình luận
                      </span>
                    )}
                    {report.status === 'Dismissed' && (
                      <span className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-700">
                        Đã giữ lại
                      </span>
                    )}
                  </td>

                  <td className="px-4 py-3 text-right whitespace-nowrap">
                    {report.status === 'Pending' ? (
                      <div className="inline-flex items-center gap-1.5">
                        <button
                          type="button"
                          onClick={() => handleOpenResolve(report, 'Dismiss')}
                          className="rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50 hover:text-slate-900 cursor-pointer shadow-2xs"
                        >
                          Giữ lại
                        </button>
                        <button
                          type="button"
                          onClick={() => handleOpenResolve(report, 'HideComment')}
                          className="rounded-lg bg-rose-600 px-2.5 py-1 text-xs font-medium text-white hover:bg-rose-700 cursor-pointer shadow-2xs"
                        >
                          Ẩn bình luận
                        </button>
                      </div>
                    ) : (
                      <span className="text-xs text-slate-400">Đã giải quyết</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Pagination */}
      {!isLoading && totalPages > 1 && (
        <div className="flex items-center justify-between pt-6">
          <span className="text-xs text-slate-500">
            Trang {page} / {totalPages} (Tổng {totalCount} báo cáo)
          </span>
          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={!hasPreviousPage || isLoading}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-40 cursor-pointer"
            >
              <ChevronLeft className="w-3.5 h-3.5" />
              Trước
            </button>
            <button
              type="button"
              disabled={!hasNextPage || isLoading}
              onClick={() => setPage((p) => p + 1)}
              className="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-40 cursor-pointer"
            >
              Sau
              <ChevronRight className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      )}

      {/* Resolve Confirmation Dialog */}
      {activeReport && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs"
          role="dialog"
          aria-modal="true"
        >
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl border border-slate-100">
            <h2 className="text-lg font-bold text-slate-900 mb-2">
              {resolveAction === 'HideComment' ? 'Ẩn bình luận vi phạm' : 'Giữ lại bình luận'}
            </h2>
            <p className="text-sm text-slate-600 mb-4">
              {resolveAction === 'HideComment'
                ? 'Bình luận này sẽ bị ẩn đối với tất cả người dùng bình thường và báo cáo sẽ được đánh dấu đã xử lý.'
                : 'Báo cáo sẽ được bác bỏ và bình luận tiếp tục hiển thị bình thường.'}
            </p>

            <div className="mb-4 rounded-xl bg-slate-50 p-3 text-xs text-slate-700 border border-slate-200">
              <span className="font-semibold block mb-1">Nội dung bình luận:</span>
              <p className="italic">&ldquo;{activeReport.commentContent}&rdquo;</p>
            </div>

            <div className="mb-4">
              <label htmlFor="resolution-notes" className="block text-xs font-semibold text-slate-700 mb-1">
                Ghi chú xử lý (tùy chọn)
              </label>
              <textarea
                id="resolution-notes"
                rows={2}
                value={resolutionNotes}
                onChange={(e) => setResolutionNotes(e.target.value)}
                placeholder="Ghi chú lý do xử lý của quản trị viên..."
                className="w-full rounded-xl border border-slate-200 p-2 text-xs text-slate-800 outline-hidden focus:border-emerald-500"
              />
            </div>

            <div className="flex justify-end gap-2">
              <button
                type="button"
                disabled={isResolving}
                onClick={() => setActiveReport(null)}
                className="rounded-xl border border-slate-200 px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-50 cursor-pointer"
              >
                Hủy
              </button>
              <button
                type="button"
                disabled={isResolving}
                onClick={handleConfirmResolve}
                className={`rounded-xl px-4 py-2 text-xs font-semibold text-white cursor-pointer ${
                  resolveAction === 'HideComment'
                    ? 'bg-rose-600 hover:bg-rose-700'
                    : 'bg-emerald-700 hover:bg-emerald-800'
                }`}
              >
                {isResolving ? 'Đang xử lý...' : 'Xác nhận'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
