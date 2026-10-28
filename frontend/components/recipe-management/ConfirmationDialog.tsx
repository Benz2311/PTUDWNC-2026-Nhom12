'use client';

import { useEffect, useRef, type ReactNode } from 'react';

export type ConfirmationDialogProps = {
  isOpen: boolean;
  title: string;
  message: ReactNode;
  confirmText?: string;
  cancelText?: string;
  variant?: 'danger' | 'warning' | 'primary';
  isLoading?: boolean;
  onConfirm: () => void | Promise<void>;
  onCancel: () => void;
};

export default function ConfirmationDialog({
  isOpen,
  title,
  message,
  confirmText = 'Xác nhận',
  cancelText = 'Hủy bỏ',
  variant = 'danger',
  isLoading = false,
  onConfirm,
  onCancel,
}: ConfirmationDialogProps) {
  const confirmBtnRef = useRef<HTMLButtonElement | null>(null);

  useEffect(() => {
    if (!isOpen) return;

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && !isLoading) {
        onCancel();
      }
    }

    window.addEventListener('keydown', handleKeyDown);
    // Focus confirmation or cancel button
    const timer = setTimeout(() => {
      confirmBtnRef.current?.focus();
    }, 50);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      clearTimeout(timer);
    };
  }, [isOpen, isLoading, onCancel]);

  if (!isOpen) return null;

  const confirmVariantClass =
    variant === 'danger'
      ? 'dialog-btn-danger'
      : variant === 'warning'
        ? 'dialog-btn-warning'
        : 'dialog-btn-primary';

  return (
    <div
      className="dialog-backdrop"
      role="presentation"
      onClick={(e) => {
        if (e.target === e.currentTarget && !isLoading) {
          onCancel();
        }
      }}
    >
      <div
        className="dialog-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="dialog-title"
        aria-describedby="dialog-description"
      >
        <div className="dialog-header">
          <div className={`dialog-icon-badge dialog-icon-${variant}`}>
            {variant === 'danger' && '⚠️'}
            {variant === 'warning' && '📦'}
            {variant === 'primary' && '✨'}
          </div>
          <h3 id="dialog-title" className="dialog-title">
            {title}
          </h3>
        </div>

        <div id="dialog-description" className="dialog-body">
          {message}
        </div>

        <div className="dialog-footer">
          <button
            type="button"
            className="outline-button dialog-btn-cancel"
            onClick={onCancel}
            disabled={isLoading}
          >
            {cancelText}
          </button>
          <button
            ref={confirmBtnRef}
            type="button"
            className={`primary-button ${confirmVariantClass}`}
            onClick={() => void onConfirm()}
            disabled={isLoading}
          >
            {isLoading ? 'Đang xử lý...' : confirmText}
          </button>
        </div>
      </div>
    </div>
  );
}
