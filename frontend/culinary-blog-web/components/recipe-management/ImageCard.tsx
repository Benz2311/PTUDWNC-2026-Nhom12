'use client';

import React from 'react';
import type { RecipeImage } from '@/types/image';

interface ImageCardProps {
  image: RecipeImage;
  index: number;
  disabled?: boolean;
  onSetPrimary: (imageId: string) => void;
  onDeleteRequest: (image: RecipeImage) => void;
  onPreview?: (image: RecipeImage) => void;
}

export function ImageCard({
  image,
  index,
  disabled = false,
  onSetPrimary,
  onDeleteRequest,
  onPreview,
}: ImageCardProps) {
  return (
    <div
      data-testid={`image-card-${index}`}
      className="group flex flex-col rounded-2xl bg-white border border-emerald-100/90 overflow-hidden shadow-xs hover:shadow-md hover:border-emerald-300 transition-all duration-300"
    >
      {/* Thumbnail Area */}
      <div
        className="relative aspect-video w-full bg-slate-100 overflow-hidden cursor-pointer"
        onClick={() => onPreview?.(image)}
        title="Nhấp để xem ảnh lớn"
      >
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={image.thumbnailUrl || image.mediumUrl || image.originalUrl}
          alt={image.altText || `Ảnh công thức ${index + 1}`}
          className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
          onError={(e) => {
            (e.currentTarget as HTMLImageElement).src =
              'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 24 24" fill="none" stroke="%23059669" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><path d="M21 15l-5-5L5 21"/></svg>';
          }}
        />

        {/* Hover overlay hint */}
        <div className="absolute inset-0 bg-emerald-950/20 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center pointer-events-none">
          <span className="px-3 py-1.5 rounded-full bg-white/90 text-emerald-900 text-xs font-semibold shadow-md backdrop-blur-xs flex items-center gap-1.5">
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
            Xem ảnh lớn
          </span>
        </div>

        {/* Primary Badge */}
        {image.isPrimary && (
          <div
            data-testid={`badge-primary-${index}`}
            className="absolute top-3 left-3 inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-700 text-white shadow-md ring-2 ring-white/90"
          >
            <svg className="w-3.5 h-3.5 text-emerald-200" fill="currentColor" viewBox="0 0 20 20">
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
            </svg>
            Ảnh đại diện
          </div>
        )}

        {/* Order Badge */}
        <div className="absolute top-3 right-3 px-2.5 py-0.5 rounded-full text-xs font-mono font-medium bg-slate-900/70 text-white backdrop-blur-md shadow-xs">
          Thứ tự: {image.orderIndex ?? index}
        </div>
      </div>

      {/* Content Area */}
      <div className="p-4 flex-1 flex flex-col justify-between gap-3">
        <div className="space-y-1.5">
          <p
            data-testid={`image-alt-${index}`}
            className="text-xs text-slate-700 line-clamp-2"
            title={image.altText || 'Không có chú thích'}
          >
            {image.altText ? (
              <span>
                <strong className="font-semibold text-slate-900">Chú thích:</strong> {image.altText}
              </span>
            ) : (
              <span className="italic text-slate-400">Chưa có chú thích (Alt text)</span>
            )}
          </p>
          <p
            className="text-[11px] text-slate-400 truncate font-mono"
            title={image.originalUrl}
          >
            {image.originalUrl}
          </p>
        </div>

        {/* Actions */}
        <div className="flex items-center justify-between gap-2 pt-3 border-t border-emerald-50">
          {!image.isPrimary ? (
            <button
              type="button"
              data-testid={`btn-set-primary-${index}`}
              disabled={disabled}
              onClick={() => onSetPrimary(image.id)}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-xl text-emerald-800 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-2xs hover:shadow-xs"
            >
              <svg className="w-3.5 h-3.5 text-emerald-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
              Đặt làm ảnh chính
            </button>
          ) : (
            <span className="text-xs font-semibold text-emerald-700 flex items-center gap-1.5 py-1">
              <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
              Đang là ảnh chính
            </span>
          )}

          <button
            type="button"
            data-testid={`btn-delete-image-${index}`}
            disabled={disabled}
            onClick={() => onDeleteRequest(image)}
            className="inline-flex items-center gap-1 px-2.5 py-1.5 text-xs font-medium rounded-xl text-slate-500 hover:text-rose-600 bg-slate-50 hover:bg-rose-50 border border-slate-200 hover:border-rose-200 disabled:opacity-40 disabled:cursor-not-allowed transition-all ml-auto"
            title="Xóa hình ảnh"
          >
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
            Xóa
          </button>
        </div>
      </div>
    </div>
  );
}

export default ImageCard;
