'use client';

import { useState, useCallback } from 'react';
import { normalizeRecipeStatus, type RecipeStatus } from '@/types/recipe';
import ConfirmationDialog from './ConfirmationDialog';

export type RecipeLifecycleAction = 'publish' | 'unpublish' | 'archive' | 'unarchive';

type RecipeActionsProps = {
  status: RecipeStatus | string | number;
  disabled: boolean;
  onAction: (action: RecipeLifecycleAction) => Promise<void> | void;
  onDelete: () => Promise<void> | void;
  publishBlocker?: string | null;
};

export default function RecipeActions({
  status,
  disabled,
  onAction,
  onDelete,
  publishBlocker = null,
}: RecipeActionsProps) {
  const [activeDialog, setActiveDialog] = useState<RecipeLifecycleAction | 'delete' | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const currentStatus = normalizeRecipeStatus(status);

  const handleConfirm = useCallback(async () => {
    if (!activeDialog) return;
    setIsProcessing(true);
    try {
      if (activeDialog === 'delete') {
        await onDelete();
      } else {
        await onAction(activeDialog);
      }
      setActiveDialog(null);
    } finally {
      setIsProcessing(false);
    }
  }, [activeDialog, onAction, onDelete]);

  return (
    <>
      <div className="recipe-actions-panel bg-emerald-50/70 border border-emerald-200/90 rounded-2xl p-5 sm:p-6 mb-6 shadow-2xs" aria-label="Thao tác vòng đời công thức">
        <div className="recipe-actions-title text-xs font-bold uppercase tracking-wider text-emerald-900 mb-3 flex items-center gap-2">
          <span className="w-2 h-2 rounded-full bg-emerald-600" />
          <span>Hành động vòng đời công thức:</span>
        </div>

        <div className="form-actions-buttons flex flex-wrap items-center gap-2.5">
          {currentStatus === 'Draft' && (
            <>
              <button
                className="primary-button action-btn-publish px-5 py-2.5 rounded-xl text-sm font-bold text-white bg-emerald-700 hover:bg-emerald-800 shadow-sm transition-all cursor-pointer disabled:opacity-50"
                disabled={disabled || Boolean(publishBlocker)}
                onClick={() => setActiveDialog('publish')}
                type="button"
                title={publishBlocker || 'Xuất bản công thức để người khác có thể xem'}
              >
                🚀 Xuất bản (Publish)
              </button>
              <button
                className="outline-button action-btn-archive px-4 py-2.5 rounded-xl text-sm font-semibold text-amber-800 bg-amber-50 hover:bg-amber-100 border border-amber-300 transition-all cursor-pointer disabled:opacity-50"
                disabled={disabled}
                onClick={() => setActiveDialog('archive')}
                type="button"
              >
                📦 Lưu trữ (Archive)
              </button>
            </>
          )}

          {currentStatus === 'Published' && (
            <>
              <button
                className="outline-button action-btn-unpublish px-4 py-2.5 rounded-xl text-sm font-semibold text-amber-800 bg-amber-50 hover:bg-amber-100 border border-amber-300 transition-all cursor-pointer disabled:opacity-50"
                disabled={disabled}
                onClick={() => setActiveDialog('unpublish')}
                type="button"
              >
                ↩ Hủy xuất bản (Unpublish)
              </button>
              <button
                className="outline-button action-btn-archive px-4 py-2.5 rounded-xl text-sm font-semibold text-gray-700 bg-gray-50 hover:bg-gray-100 border border-gray-300 transition-all cursor-pointer disabled:opacity-50"
                disabled={disabled}
                onClick={() => setActiveDialog('archive')}
                type="button"
              >
                📦 Lưu trữ (Archive)
              </button>
            </>
          )}

          {currentStatus === 'Archived' && (
            <button
              className="primary-button action-btn-unarchive px-5 py-2.5 rounded-xl text-sm font-bold text-white bg-emerald-700 hover:bg-emerald-800 shadow-sm transition-all cursor-pointer disabled:opacity-50"
              disabled={disabled}
              onClick={() => setActiveDialog('unarchive')}
              type="button"
            >
              🔓 Mở lưu trữ về Nháp (Unarchive)
            </button>
          )}

          <button
            className="delete-action action-btn-delete px-4 py-2.5 rounded-xl text-sm font-semibold text-rose-700 bg-rose-50 hover:bg-rose-100 border border-rose-200 transition-all cursor-pointer disabled:opacity-50 ml-auto"
            disabled={disabled}
            onClick={() => setActiveDialog('delete')}
            type="button"
          >
            🗑️ Xóa công thức
          </button>
        </div>

        {publishBlocker && currentStatus === 'Draft' && (
          <div className="publish-blocker-note mt-3 p-3 rounded-xl bg-amber-50 border border-amber-200 text-xs font-semibold text-amber-800 flex items-center gap-2" role="alert">
            <span>⚠️ Chưa đủ điều kiện xuất bản: {publishBlocker}</span>
          </div>
        )}
      </div>

      {/* Confirmation Dialogs for each transition */}
      <ConfirmationDialog
        isOpen={activeDialog === 'publish'}
        title="Xác nhận xuất bản công thức"
        message={
          <div>
            <p>
              Bạn có chắc chắn muốn xuất bản công thức này lên cộng đồng Culinary Blog?
            </p>
            <p style={{ marginTop: 8, fontSize: 13, color: 'var(--ink-soft)' }}>
              Sau khi xuất bản, bất kỳ ai cũng có thể tìm kiếm và xem công thức của bạn.
            </p>
          </div>
        }
        confirmText="Xuất bản ngay"
        cancelText="Để sau"
        variant="primary"
        isLoading={isProcessing}
        onConfirm={handleConfirm}
        onCancel={() => !isProcessing && setActiveDialog(null)}
      />

      <ConfirmationDialog
        isOpen={activeDialog === 'unpublish'}
        title="Xác nhận hủy xuất bản"
        message={
          <div>
            <p>
              Chuyển công thức này về trạng thái <strong>Bản nháp (Draft)</strong>?
            </p>
            <p style={{ marginTop: 8, fontSize: 13, color: 'var(--ink-soft)' }}>
              Công thức sẽ bị ẩn khỏi cộng đồng và chỉ có bạn hoặc Quản trị viên mới xem được.
            </p>
          </div>
        }
        confirmText="Hủy xuất bản"
        cancelText="Đóng"
        variant="warning"
        isLoading={isProcessing}
        onConfirm={handleConfirm}
        onCancel={() => !isProcessing && setActiveDialog(null)}
      />

      <ConfirmationDialog
        isOpen={activeDialog === 'archive'}
        title="Xác nhận lưu trữ công thức"
        message={
          <div>
            <p>
              Chuyển công thức này vào danh sách <strong>Lưu trữ (Archived)</strong>?
            </p>
            <p style={{ marginTop: 8, fontSize: 13, color: 'var(--ink-soft)' }}>
              Công thức sẽ ngừng xuất hiện trên các trang khám phá và tìm kiếm công khai.
            </p>
          </div>
        }
        confirmText="Lưu trữ"
        cancelText="Hủy"
        variant="warning"
        isLoading={isProcessing}
        onConfirm={handleConfirm}
        onCancel={() => !isProcessing && setActiveDialog(null)}
      />

      <ConfirmationDialog
        isOpen={activeDialog === 'unarchive'}
        title="Xác nhận mở lại lưu trữ"
        message={
          <div>
            <p>
              Mở lại công thức này? Công thức sẽ được chuyển về trạng thái <strong>Bản nháp (Draft)</strong> để bạn tiếp tục chỉnh sửa.
            </p>
          </div>
        }
        confirmText="Mở lưu trữ"
        cancelText="Đóng"
        variant="primary"
        isLoading={isProcessing}
        onConfirm={handleConfirm}
        onCancel={() => !isProcessing && setActiveDialog(null)}
      />

      <ConfirmationDialog
        isOpen={activeDialog === 'delete'}
        title="Xác nhận xóa công thức"
        message={
          <div>
            <p>
              Bạn có chắc chắn muốn chuyển công thức này vào <strong>Thùng rác</strong>?
            </p>
            <p style={{ marginTop: 8, fontSize: 13, color: 'var(--ink-soft)' }}>
              Công thức sẽ bị xóa mềm và có thể khôi phục bởi Quản trị viên trong vòng 30 ngày.
            </p>
          </div>
        }
        confirmText="Xóa công thức"
        cancelText="Giữ lại"
        variant="danger"
        isLoading={isProcessing}
        onConfirm={handleConfirm}
        onCancel={() => !isProcessing && setActiveDialog(null)}
      />
    </>
  );
}

