'use client';

import { useState } from 'react';
import { Flag, X, AlertTriangle } from 'lucide-react';
import { reportComment } from '@/lib/api/comments';
import { useToast } from '@/components/ui/toast';

interface ReportCommentModalProps {
  commentId: string;
  isOpen: boolean;
  onClose: () => void;
}

const REPORT_REASONS = [
  'Spam / Quảng cáo rác',
  'Quấy rối / Công kích cá nhân',
  'Nội dung phản cảm / Thô tục',
  'Thông tin sai lệch / Gây hiểu lầm',
  'Lý do khác',
] as const;

export default function ReportCommentModal({
  commentId,
  isOpen,
  onClose,
}: ReportCommentModalProps) {
  const toast = useToast();
  const [reason, setReason] = useState<string>(REPORT_REASONS[0]);
  const [details, setDetails] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) {
      setErrorMsg('Vui lòng chọn lý do báo cáo.');
      return;
    }

    setIsSubmitting(true);
    setErrorMsg(null);

    try {
      await reportComment(commentId, reason, details.trim() || undefined);
      toast?.success?.('Cảm ơn bạn! Báo cáo đã được gửi tới ban quản trị để xem xét.');
      onClose();
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { message?: string } }; message?: string };
      const msg =
        anyErr?.response?.data?.message ||
        anyErr?.message ||
        'Không thể gửi báo cáo vào lúc này.';
      setErrorMsg(msg);
      toast?.error?.(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs"
      role="dialog"
      aria-modal="true"
      aria-labelledby="report-modal-title"
    >
      <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl border border-slate-100 animate-in fade-in zoom-in-95">
        <div className="flex items-center justify-between pb-3 border-b border-slate-100 mb-4">
          <div className="flex items-center gap-2 text-rose-600 font-semibold">
            <Flag className="w-5 h-5" />
            <h2 id="report-modal-title" className="text-lg text-slate-900">
              Báo cáo bình luận
            </h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 cursor-pointer"
            aria-label="Đóng modal"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {errorMsg && (
          <div className="mb-4 flex items-start gap-2 rounded-xl bg-red-50 p-3 text-sm text-red-700 border border-red-200">
            <AlertTriangle className="w-4 h-4 mt-0.5 shrink-0 text-red-600" />
            <span>{errorMsg}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-2">
              Lý do báo cáo vi phạm <span className="text-red-500">*</span>
            </label>
            <div className="space-y-2">
              {REPORT_REASONS.map((r) => (
                <label
                  key={r}
                  className={`flex items-center gap-2 p-2.5 rounded-xl border cursor-pointer text-sm transition ${
                    reason === r
                      ? 'border-emerald-600 bg-emerald-50/50 text-emerald-900 font-medium'
                      : 'border-slate-200 hover:bg-slate-50 text-slate-700'
                  }`}
                >
                  <input
                    type="radio"
                    name="reportReason"
                    value={r}
                    checked={reason === r}
                    onChange={() => setReason(r)}
                    className="accent-emerald-600"
                  />
                  <span>{r}</span>
                </label>
              ))}
            </div>
          </div>

          <div>
            <label htmlFor="report-details" className="block text-sm font-medium text-slate-700 mb-1">
              Mô tả chi tiết (không bắt buộc)
            </label>
            <textarea
              id="report-details"
              rows={3}
              value={details}
              onChange={(e) => setDetails(e.target.value)}
              placeholder="Cung cấp thêm ngữ cảnh giúp ban quản trị xử lý nhanh hơn..."
              className="w-full rounded-xl border border-slate-200 p-2.5 text-sm text-slate-800 placeholder-slate-400 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-hidden"
              maxLength={1000}
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-xl bg-rose-600 px-4 py-2 text-sm font-medium text-white hover:bg-rose-700 disabled:opacity-50 cursor-pointer"
            >
              {isSubmitting ? 'Đang gửi...' : 'Gửi báo cáo'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
