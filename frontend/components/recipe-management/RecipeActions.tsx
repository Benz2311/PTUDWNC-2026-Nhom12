'use client';

import { useState } from 'react';
import type { RecipeStatus } from '@/types/recipe';
import ConfirmationDialog from './ConfirmationDialog';

export type RecipeLifecycleAction = 'publish' | 'unpublish' | 'archive' | 'unarchive';

type RecipeActionsProps = {
  status: RecipeStatus;
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

  async function handleConfirm() {
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
  }

  return (
    <>
      <div className="recipe-actions-panel" aria-label="Thao tác vòng đời công thức">
        <div className="recipe-actions-title">
          <span>Hành động vòng đời công thức:</span>
        </div>

        <div className="form-actions-buttons">
          {status === 'Draft' && (
            <>
              <button
                className="primary-button action-btn-publish"
                disabled={disabled || Boolean(publishBlocker)}
                onClick={() => setActiveDialog('publish')}
                type="button"
                title={publishBlocker || 'Xuất bản công thức để người khác có thể xem'}
              >
                🚀 Xuất bản (Publish)
              </button>
              <button
                className="outline-button action-btn-archive"
                disabled={disabled}
                onClick={() => setActiveDialog('archive')}
                type="button"
              >
                📦 Lưu trữ (Archive)
              </button>
            </>
          )}

          {status === 'Published' && (
            <>
              <button
                className="outline-button action-btn-unpublish"
                disabled={disabled}
                onClick={() => setActiveDialog('unpublish')}
                type="button"
              >
                ↩ Hủy xuất bản (Unpublish)
              </button>
              <button
                className="outline-button action-btn-archive"
                disabled={disabled}
                onClick={() => setActiveDialog('archive')}
                type="button"
              >
                📦 Lưu trữ (Archive)
              </button>
            </>
          )}

          {status === 'Archived' && (
            <button
              className="primary-button action-btn-unarchive"
              disabled={disabled}
              onClick={() => setActiveDialog('unarchive')}
              type="button"
            >
              🔓 Mở lưu trữ về Nháp (Unarchive)
            </button>
          )}

          <button
            className="delete-action action-btn-delete"
            disabled={disabled}
            onClick={() => setActiveDialog('delete')}
            type="button"
          >
            🗑️ Xóa công thức
          </button>
        </div>

        {publishBlocker && status === 'Draft' && (
          <div className="publish-blocker-note" role="alert">
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