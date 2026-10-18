'use client';

import React from 'react';
import type { RecipeImage } from '@/types/image';

export interface ImagePreviewProps {
  image: RecipeImage | null;
  isOpen: boolean;
  onClose: () => void;
  onSetPrimary?: (imageId: string) => void;
  onDeleteRequest?: (image: RecipeImage) => void;
  disabled?: boolean;
}

export function ImagePreview({
  image,
  isOpen,
  onClose,
  onSetPrimary,
  onDeleteRequest,
  disabled = false,
}: ImagePreviewProps) {
  if (!isOpen || !image) return null;

  return (
    <div
      data-testid="image-preview-modal"
      role="dialog"
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-xs"
      onClick={onClose}
    >
      <div
        className="relative max-w-3xl w-full bg-white rounded-2xl overflow-hidden shadow-2xl border border-gray-200"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-3.5 border-b border-gray-100 bg-gray-50/80">
          <div className="flex items-center gap-2">
            <h4 className="text-sm font-semibold text-gray-900 truncate max-w-md">
              {image.altText || 'Xem trước hình ảnh'}
            </h4>
            {image.isPrimary && (
              <span
                data-testid="preview-primary-badge"
                className="px-2 py-0.5 rounded text-[11px] font-bold bg-green-100 text-green-800"
              >
                Ảnh đại diện
              </span>
            )}
          </div>

          <button
            type="button"
            data-testid="btn-close-preview"
            onClick={onClose}
            className="p-1 rounded-lg text-gray-400 hover:text-gray-700 hover:bg-gray-200/50 transition-colors"
            title="Đóng"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Image Display */}
        <div className="relative aspect-video w-full bg-neutral-900 flex items-center justify-center overflow-hidden">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={image.mediumUrl || image.originalUrl}
            alt={image.altText || 'Chi tiết hình ảnh công thức'}
            className="max-h-[70vh] w-auto max-w-full object-contain"
          />
        </div>

        {/* Footer info & actions */}
        <div className="p-4 bg-white flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs border-t border-gray-100">
          <div className="space-y-0.5 text-gray-500">
            {image.altText && (
              <p className="text-gray-700 font-medium line-clamp-1">
                <strong>Chú thích:</strong> {image.altText}
              </p>
            )}
            <p className="truncate max-w-xs sm:max-w-md font-mono text-[11px]">
              {image.originalUrl}
            </p>
          </div>

          <div className="flex items-center gap-2 shrink-0">
            {!image.isPrimary && onSetPrimary && (
              <button
                type="button"
                data-testid="btn-preview-set-primary"
                disabled={disabled}
                onClick={() => {
                  onSetPrimary(image.id);
                  onClose();
                }}
                className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg text-xs font-semibold bg-green-50 text-green-800 border border-green-200 hover:bg-green-100 disabled:opacity-50 transition-colors"
              >
                Đặt làm ảnh chính
              </button>
            )}

            {onDeleteRequest && (
              <button
                type="button"
                data-testid="btn-preview-delete"
                disabled={disabled}
                onClick={() => {
                  onDeleteRequest(image);
                  onClose();
                }}
                className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg text-xs font-semibold bg-red-50 text-red-700 border border-red-200 hover:bg-red-100 disabled:opacity-50 transition-colors"
              >
                Xóa ảnh
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default ImagePreview;
