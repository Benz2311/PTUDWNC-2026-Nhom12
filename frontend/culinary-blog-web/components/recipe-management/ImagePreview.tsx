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
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/75 backdrop-blur-sm animate-fade-in"
      onClick={onClose}
    >
      <div
        className="relative max-w-3xl w-full bg-white rounded-3xl overflow-hidden shadow-2xl border border-emerald-100"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-emerald-50 bg-emerald-50/40">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-800 shrink-0">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <div>
              <h4 className="text-sm font-bold text-slate-900 truncate max-w-sm sm:max-w-md">
                {image.altText || 'Xem trước hình ảnh'}
              </h4>
              <p className="text-[11px] text-slate-500 font-mono truncate max-w-xs">
                {image.originalUrl}
              </p>
            </div>
            {image.isPrimary && (
              <span
                data-testid="preview-primary-badge"
                className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-emerald-700 text-white shadow-xs ml-2"
              >
                Ảnh đại diện
              </span>
            )}
          </div>

          <button
            type="button"
            data-testid="btn-close-preview"
            onClick={onClose}
            className="p-1.5 rounded-xl text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition-colors"
            title="Đóng"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Image Display */}
        <div className="relative aspect-video w-full bg-slate-950 flex items-center justify-center overflow-hidden">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={image.mediumUrl || image.originalUrl}
            alt={image.altText || 'Chi tiết hình ảnh công thức'}
            className="max-h-[65vh] w-auto max-w-full object-contain"
          />
        </div>

        {/* Footer info & actions */}
        <div className="p-4 sm:p-5 bg-white flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs border-t border-emerald-50">
          <div className="space-y-0.5 text-slate-500">
            {image.altText && (
              <p className="text-slate-800 font-medium line-clamp-1">
                <strong className="text-slate-900">Chú thích:</strong> {image.altText}
              </p>
            )}
            <p className="text-emerald-700 text-[11px] font-medium">
              Chất lượng ảnh gốc HD • Định dạng tối ưu Web
            </p>
          </div>

          <div className="flex items-center gap-2.5 shrink-0">
            {!image.isPrimary && onSetPrimary && (
              <button
                type="button"
                data-testid="btn-preview-set-primary"
                disabled={disabled}
                onClick={() => {
                  onSetPrimary(image.id);
                  onClose();
                }}
                className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold bg-emerald-50 text-emerald-800 border border-emerald-200 hover:bg-emerald-100 disabled:opacity-50 transition-all shadow-2xs"
              >
                <svg className="w-3.5 h-3.5 text-emerald-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
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
                className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold bg-rose-50 text-rose-700 border border-rose-200 hover:bg-rose-100 disabled:opacity-50 transition-all"
              >
                <svg className="w-3.5 h-3.5 text-rose-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
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
