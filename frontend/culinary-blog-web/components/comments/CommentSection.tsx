'use client';

import { useCallback, useEffect, useState } from 'react';
import Link from 'next/link';
import {
  MessageSquare,
  CornerDownRight,
  Edit2,
  Trash2,
  Flag,
  Send,
  Loader2,
  AlertCircle,
  Check,
  X,
  ChevronLeft,
  ChevronRight,
  ShieldAlert,
} from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';
import { getRolesFromToken } from '@/lib/jwt';
import {
  getComments,
  createComment,
  updateComment,
  deleteComment,
} from '@/lib/api/comments';
import type { CommentItem } from '@/types/comment';
import { useToast } from '@/components/ui/toast';
import ReportCommentModal from './ReportCommentModal';

interface CommentSectionProps {
  recipeId: string;
  recipeStatus: string;
}

export default function CommentSection({
  recipeId,
  recipeStatus,
}: CommentSectionProps) {
  const { user, accessToken, isAuthenticated } = useAuth();
  const toast = useToast();
  const isAdmin = getRolesFromToken(accessToken).some(
    (role) => role.toLowerCase() === 'admin'
  );

  const isPublished =
    recipeStatus.toLowerCase() === 'published' || recipeStatus === '1';

  const [comments, setComments] = useState<CommentItem[]>([]);
  const [page, setPage] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [hasNextPage, setHasNextPage] = useState<boolean>(false);
  const [hasPreviousPage, setHasPreviousPage] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // New comment input
  const [newCommentText, setNewCommentText] = useState<string>('');
  const [isPosting, setIsPosting] = useState<boolean>(false);

  // Reply input state
  const [replyingToId, setReplyingToId] = useState<string | null>(null);
  const [replyText, setReplyText] = useState<string>('');
  const [isSubmittingReply, setIsSubmittingReply] = useState<boolean>(false);

  // Edit input state
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editingText, setEditingText] = useState<string>('');
  const [isSubmittingEdit, setIsSubmittingEdit] = useState<boolean>(false);

  // Report modal state
  const [reportingCommentId, setReportingCommentId] = useState<string | null>(null);

  const fetchComments = useCallback(
    async (pageToLoad: number) => {
      setIsLoading(true);
      setError(null);
      try {
        const result = await getComments(recipeId, pageToLoad, 10);
        setComments(result.items);
        setPage(result.page);
        setTotalPages(result.totalPages);
        setTotalCount(result.totalCount);
        setHasNextPage(result.hasNextPage);
        setHasPreviousPage(result.hasPreviousPage);
      } catch (err: unknown) {
        const anyErr = err as { message?: string };
        setError(anyErr?.message || 'Không thể tải danh sách bình luận.');
      } finally {
        setIsLoading(false);
      }
    },
    [recipeId]
  );

  useEffect(() => {
    fetchComments(1);
  }, [fetchComments]);

  // Create root comment
  const handleCreateComment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isAuthenticated) {
      toast?.info?.('Vui lòng đăng nhập để gửi bình luận.');
      return;
    }

    const trimmed = newCommentText.trim();
    if (!trimmed) return;

    setIsPosting(true);
    try {
      await createComment(recipeId, trimmed);
      setNewCommentText('');
      toast?.success?.('Đã đăng bình luận thành công!');
      // Reload first page
      fetchComments(1);
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { message?: string } }; message?: string };
      const msg =
        anyErr?.response?.data?.message ||
        anyErr?.message ||
        'Không thể gửi bình luận.';
      toast?.error?.(msg);
    } finally {
      setIsPosting(false);
    }
  };

  // Create reply
  const handleSendReply = async (parentCommentId: string) => {
    if (!isAuthenticated) {
      toast?.info?.('Vui lòng đăng nhập để trả lời bình luận.');
      return;
    }

    const trimmed = replyText.trim();
    if (!trimmed) return;

    setIsSubmittingReply(true);
    try {
      await createComment(recipeId, trimmed, parentCommentId);
      setReplyText('');
      setReplyingToId(null);
      toast?.success?.('Đã gửi câu trả lời!');
      fetchComments(page);
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { message?: string } }; message?: string };
      const msg =
        anyErr?.response?.data?.message ||
        anyErr?.message ||
        'Không thể gửi câu trả lời.';
      toast?.error?.(msg);
    } finally {
      setIsSubmittingReply(false);
    }
  };

  // Update comment
  const handleSaveEdit = async (commentId: string) => {
    const trimmed = editingText.trim();
    if (!trimmed) return;

    setIsSubmittingEdit(true);
    try {
      await updateComment(commentId, trimmed);
      setEditingCommentId(null);
      setEditingText('');
      toast?.success?.('Đã cập nhật bình luận!');
      fetchComments(page);
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { message?: string } }; message?: string };
      const msg =
        anyErr?.response?.data?.message ||
        anyErr?.message ||
        'Không thể cập nhật bình luận.';
      toast?.error?.(msg);
    } finally {
      setIsSubmittingEdit(false);
    }
  };

  // Delete comment
  const handleDeleteComment = async (commentId: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa bình luận này không?')) return;

    try {
      await deleteComment(commentId);
      toast?.success?.('Đã xóa bình luận thành công.');
      fetchComments(page);
    } catch (err: unknown) {
      const anyErr = err as { response?: { data?: { message?: string } }; message?: string };
      const msg =
        anyErr?.response?.data?.message ||
        anyErr?.message ||
        'Không thể xóa bình luận.';
      toast?.error?.(msg);
    }
  };

  const formatDate = (dateStr: string) => {
    try {
      const date = new Date(dateStr);
      return new Intl.DateTimeFormat('vi-VN', {
        dateStyle: 'medium',
        timeStyle: 'short',
      }).format(date);
    } catch {
      return dateStr;
    }
  };

  return (
    <section className="mt-12 pt-8 border-t border-slate-200" aria-label="Khu vực bình luận">
      <div className="flex items-center gap-2 mb-6">
        <MessageSquare className="w-6 h-6 text-emerald-700" />
        <h2 className="text-2xl font-bold text-slate-900">
          Bình luận {totalCount > 0 && <span className="text-slate-500 font-normal">({totalCount})</span>}
        </h2>
      </div>

      {/* Form đăng bình luận mới */}
      {isPublished ? (
        isAuthenticated ? (
          <form onSubmit={handleCreateComment} className="mb-8">
            <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-2xs focus-within:border-emerald-500 focus-within:ring-1 focus-within:ring-emerald-500">
              <label htmlFor="new-comment" className="sr-only">
                Nhập nội dung bình luận
              </label>
              <textarea
                id="new-comment"
                rows={3}
                value={newCommentText}
                onChange={(e) => setNewCommentText(e.target.value)}
                placeholder="Chia sẻ cảm nghĩ, mẹo nấu hoặc thắc mắc của bạn về công thức này..."
                className="w-full resize-y text-sm text-slate-800 placeholder-slate-400 outline-hidden bg-transparent"
                maxLength={1000}
                required
              />
              <div className="flex items-center justify-between pt-2 border-t border-slate-100 mt-2">
                <span className="text-xs text-slate-400">
                  {newCommentText.length}/1000 ký tự
                </span>
                <button
                  type="submit"
                  disabled={isPosting || !newCommentText.trim()}
                  className="inline-flex items-center gap-1.5 rounded-xl bg-emerald-700 px-4 py-2 text-sm font-semibold text-white shadow-2xs hover:bg-emerald-800 disabled:opacity-50 cursor-pointer transition"
                >
                  {isPosting ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      Đang gửi...
                    </>
                  ) : (
                    <>
                      <Send className="w-4 h-4" />
                      Gửi bình luận
                    </>
                  )}
                </button>
              </div>
            </div>
          </form>
        ) : (
          <div className="mb-8 rounded-2xl bg-slate-50 border border-slate-200 p-4 text-center text-sm text-slate-600">
            <span>
              Vui lòng{' '}
              <Link href="/login" className="font-semibold text-emerald-700 hover:underline">
                Đăng nhập
              </Link>{' '}
              hoặc{' '}
              <Link href="/register" className="font-semibold text-emerald-700 hover:underline">
                Đăng ký tài khoản
              </Link>{' '}
              để tham gia bình luận về món ăn này.
            </span>
          </div>
        )
      ) : (
        <div className="mb-8 rounded-2xl bg-amber-50 border border-amber-200 p-4 text-sm text-amber-800">
          Chỉ có thể bình luận khi công thức đã được xuất bản (Published).
        </div>
      )}

      {/* Loading state */}
      {isLoading && (
        <div className="space-y-4 py-6" role="status" aria-label="Đang tải bình luận">
          {[1, 2, 3].map((n) => (
            <div key={n} className="flex gap-3 animate-pulse">
              <div className="w-10 h-10 rounded-full bg-slate-200 shrink-0" />
              <div className="flex-1 space-y-2">
                <div className="w-32 h-4 bg-slate-200 rounded" />
                <div className="w-full h-12 bg-slate-200 rounded-xl" />
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Error state */}
      {!isLoading && error && (
        <div className="rounded-2xl bg-red-50 p-6 text-center border border-red-200 my-6">
          <AlertCircle className="w-8 h-8 text-red-500 mx-auto mb-2" />
          <p className="text-sm font-medium text-red-800 mb-3">{error}</p>
          <button
            type="button"
            onClick={() => fetchComments(page)}
            className="rounded-xl bg-white border border-red-200 px-4 py-2 text-xs font-semibold text-red-700 hover:bg-red-50 cursor-pointer shadow-2xs"
          >
            Thử lại
          </button>
        </div>
      )}

      {/* Empty state */}
      {!isLoading && !error && comments.length === 0 && (
        <div className="rounded-2xl border border-dashed border-slate-200 py-12 text-center my-6">
          <MessageSquare className="w-10 h-10 text-slate-300 mx-auto mb-2" />
          <h3 className="text-base font-semibold text-slate-800 mb-1">
            Chưa có bình luận nào
          </h3>
          <p className="text-sm text-slate-500 max-w-sm mx-auto">
            Hãy là người đầu tiên chia sẻ cảm nghĩ hoặc đặt câu hỏi về công thức này!
          </p>
        </div>
      )}

      {/* Comments List */}
      {!isLoading && !error && comments.length > 0 && (
        <div className="space-y-6">
          {comments.map((comment) => {
            const isAuthor = user != null && user.id === comment.author.id;
            const canModify = isAuthor;
            const canDelete = isAuthor || isAdmin;

            return (
              <div
                key={comment.id}
                className={`rounded-2xl border ${
                  comment.isHidden ? 'bg-slate-50 border-slate-200' : 'bg-white border-slate-100 shadow-2xs'
                } p-4 transition`}
              >
                {/* Header: Author + Date */}
                <div className="flex items-start justify-between gap-3 mb-2">
                  <div className="flex items-center gap-3">
                    <div className="w-9 h-9 rounded-full bg-emerald-100 text-emerald-800 flex items-center justify-center font-bold text-sm shrink-0 overflow-hidden">
                      {comment.author.avatarUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img
                          src={comment.author.avatarUrl}
                          alt={comment.author.displayName}
                          className="w-full h-full object-cover"
                        />
                      ) : (
                        comment.author.displayName.charAt(0).toUpperCase()
                      )}
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-semibold text-sm text-slate-900">
                          {comment.author.displayName}
                        </span>
                        {comment.isHidden && (
                          <span className="inline-flex items-center gap-1 rounded-md bg-amber-100 px-1.5 py-0.5 text-3xs font-medium text-amber-800">
                            <ShieldAlert className="w-3 h-3" /> Đã ẩn
                          </span>
                        )}
                      </div>
                      <time className="text-xs text-slate-400">
                        {formatDate(comment.createdAt)}
                        {comment.updatedAt && ' (đã chỉnh sửa)'}
                      </time>
                    </div>
                  </div>

                  {/* Action buttons */}
                  <div className="flex items-center gap-1 text-slate-400">
                    {isAuthenticated && !comment.isHidden && (
                      <button
                        type="button"
                        onClick={() => {
                          setReplyingToId(replyingToId === comment.id ? null : comment.id);
                          setReplyText('');
                        }}
                        className="p-1.5 rounded-lg hover:bg-slate-100 hover:text-emerald-700 text-xs flex items-center gap-1 cursor-pointer"
                        title="Trả lời bình luận"
                      >
                        <CornerDownRight className="w-3.5 h-3.5" />
                        <span className="hidden sm:inline">Trả lời</span>
                      </button>
                    )}

                    {canModify && !comment.isHidden && (
                      <button
                        type="button"
                        onClick={() => {
                          setEditingCommentId(comment.id);
                          setEditingText(comment.content);
                        }}
                        className="p-1.5 rounded-lg hover:bg-slate-100 hover:text-blue-600 cursor-pointer"
                        title="Chỉnh sửa bình luận của bạn"
                      >
                        <Edit2 className="w-3.5 h-3.5" />
                      </button>
                    )}

                    {canDelete && (
                      <button
                        type="button"
                        onClick={() => handleDeleteComment(comment.id)}
                        className="p-1.5 rounded-lg hover:bg-slate-100 hover:text-red-600 cursor-pointer"
                        title="Xóa bình luận"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    )}

                    {isAuthenticated && !isAuthor && (
                      <button
                        type="button"
                        onClick={() => setReportingCommentId(comment.id)}
                        className="p-1.5 rounded-lg hover:bg-slate-100 hover:text-rose-600 cursor-pointer"
                        title="Báo cáo bình luận vi phạm"
                      >
                        <Flag className="w-3.5 h-3.5" />
                      </button>
                    )}
                  </div>
                </div>

                {/* Content or Edit form */}
                {editingCommentId === comment.id ? (
                  <div className="mt-2 space-y-2">
                    <textarea
                      rows={2}
                      value={editingText}
                      onChange={(e) => setEditingText(e.target.value)}
                      className="w-full rounded-xl border border-emerald-500 p-2.5 text-sm text-slate-800 outline-hidden"
                      maxLength={1000}
                    />
                    <div className="flex justify-end gap-2">
                      <button
                        type="button"
                        onClick={() => setEditingCommentId(null)}
                        className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 cursor-pointer"
                      >
                        <X className="w-3 h-3 inline mr-1" />
                        Hủy
                      </button>
                      <button
                        type="button"
                        disabled={isSubmittingEdit || !editingText.trim()}
                        onClick={() => handleSaveEdit(comment.id)}
                        className="rounded-lg bg-emerald-700 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-800 disabled:opacity-50 cursor-pointer"
                      >
                        <Check className="w-3 h-3 inline mr-1" />
                        Lưu thay đổi
                      </button>
                    </div>
                  </div>
                ) : (
                  <p
                    className={`text-sm leading-relaxed ${
                      comment.isHidden ? 'italic text-slate-400' : 'text-slate-700'
                    }`}
                  >
                    {comment.content}
                  </p>
                )}

                {/* Inline Reply Form */}
                {replyingToId === comment.id && (
                  <div className="mt-3 pt-3 border-t border-slate-100 pl-4 border-l-2 border-l-emerald-500">
<textarea
                        rows={2}
                        value={replyText}
                        onChange={(e) => setReplyText(e.target.value)}
                        placeholder={`Trả lời @${comment.author.displayName}...`}
                        className="w-full rounded-xl border border-slate-200 p-2.5 text-sm text-slate-800 outline-hidden focus:border-emerald-500"
                        maxLength={1000}
                      />
                    <div className="flex justify-end gap-2 mt-2">
                      <button
                        type="button"
                        onClick={() => setReplyingToId(null)}
                        className="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 cursor-pointer"
                      >
                        Hủy
                      </button>
                      <button
                        type="button"
                        disabled={isSubmittingReply || !replyText.trim()}
                        onClick={() => handleSendReply(comment.id)}
                        className="rounded-lg bg-emerald-700 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-800 disabled:opacity-50 cursor-pointer"
                      >
                        {isSubmittingReply ? 'Đang gửi...' : 'Trả lời'}
                      </button>
                    </div>
                  </div>
                )}

                {/* Replies List (Cấu trúc một cấp) */}
                {comment.replies && comment.replies.length > 0 && (
                  <div className="mt-4 pl-4 sm:pl-6 border-l-2 border-emerald-100 space-y-3">
                    {comment.replies.map((reply) => {
                      const isReplyAuthor = user != null && user.id === reply.author.id;
                      const canModifyReply = isReplyAuthor;
                      const canDeleteReply = isReplyAuthor || isAdmin;

                      return (
                        <div
                          key={reply.id}
                          className={`rounded-xl p-3 ${
                            reply.isHidden ? 'bg-slate-100 text-slate-400 italic' : 'bg-slate-50 text-slate-700'
                          }`}
                        >
                          <div className="flex items-start justify-between gap-2 mb-1">
                            <div className="flex items-center gap-2">
                              <div className="w-6 h-6 rounded-full bg-emerald-200 text-emerald-900 flex items-center justify-center font-bold text-xs shrink-0 overflow-hidden">
                                {reply.author.avatarUrl ? (
                                  // eslint-disable-next-line @next/next/no-img-element
                                  <img
                                    src={reply.author.avatarUrl}
                                    alt={reply.author.displayName}
                                    className="w-full h-full object-cover"
                                  />
                                ) : (
                                  reply.author.displayName.charAt(0).toUpperCase()
                                )}
                              </div>
                              <span className="font-semibold text-xs text-slate-900">
                                {reply.author.displayName}
                              </span>
                              <time className="text-3xs text-slate-400">
                                {formatDate(reply.createdAt)}
                              </time>
                            </div>

                            <div className="flex items-center gap-1 text-slate-400">
                              {canModifyReply && !reply.isHidden && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    setEditingCommentId(reply.id);
                                    setEditingText(reply.content);
                                  }}
                                  className="p-1 hover:text-blue-600 cursor-pointer"
                                  title="Chỉnh sửa câu trả lời"
                                >
                                  <Edit2 className="w-3 h-3" />
                                </button>
                              )}

                              {canDeleteReply && (
                                <button
                                  type="button"
                                  onClick={() => handleDeleteComment(reply.id)}
                                  className="p-1 hover:text-red-600 cursor-pointer"
                                  title="Xóa câu trả lời"
                                >
                                  <Trash2 className="w-3 h-3" />
                                </button>
                              )}

                              {isAuthenticated && !isReplyAuthor && (
                                <button
                                  type="button"
                                  onClick={() => setReportingCommentId(reply.id)}
                                  className="p-1 hover:text-rose-600 cursor-pointer"
                                  title="Báo cáo vi phạm"
                                >
                                  <Flag className="w-3 h-3" />
                                </button>
                              )}
                            </div>
                          </div>

                          {editingCommentId === reply.id ? (
                            <div className="mt-2 space-y-2">
                              <textarea
                                rows={2}
                                value={editingText}
                                onChange={(e) => setEditingText(e.target.value)}
                                className="w-full rounded-xl border border-emerald-500 p-2 text-xs text-slate-800 outline-hidden bg-white"
                                maxLength={1000}
                              />
                              <div className="flex justify-end gap-1.5">
                                <button
                                  type="button"
                                  onClick={() => setEditingCommentId(null)}
                                  className="rounded-md border border-slate-200 px-2.5 py-1 text-2xs text-slate-600 hover:bg-slate-100 cursor-pointer"
                                >
                                  Hủy
                                </button>
                                <button
                                  type="button"
                                  disabled={isSubmittingEdit || !editingText.trim()}
                                  onClick={() => handleSaveEdit(reply.id)}
                                  className="rounded-md bg-emerald-700 px-2.5 py-1 text-2xs font-semibold text-white hover:bg-emerald-800 cursor-pointer"
                                >
                                  Lưu
                                </button>
                              </div>
                            </div>
                          ) : (
                            <p className="text-xs leading-relaxed">{reply.content}</p>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}

          {/* Phân trang (Pagination) */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4 border-t border-slate-100">
              <span className="text-xs text-slate-500">
                Trang {page} / {totalPages} (Tổng {totalCount} bình luận)
              </span>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  disabled={!hasPreviousPage || isLoading}
                  onClick={() => fetchComments(page - 1)}
                  className="inline-flex items-center gap-1 rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-40 cursor-pointer"
                >
                  <ChevronLeft className="w-3.5 h-3.5" />
                  Trước
                </button>
                <button
                  type="button"
                  disabled={!hasNextPage || isLoading}
                  onClick={() => fetchComments(page + 1)}
                  className="inline-flex items-center gap-1 rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-40 cursor-pointer"
                >
                  Sau
                  <ChevronRight className="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Modal Báo cáo bình luận */}
      {reportingCommentId && (
        <ReportCommentModal
          commentId={reportingCommentId}
          isOpen={Boolean(reportingCommentId)}
          onClose={() => setReportingCommentId(null)}
        />
      )}
    </section>
  );
}
