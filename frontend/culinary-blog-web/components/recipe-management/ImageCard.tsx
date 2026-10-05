'use client';

import React from 'react';
import type { RecipeImage } from '@/types/image';

interface ImageCardProps {
  image: RecipeImage;
  index: number;
  disabled?: boolean;
  onSetPrimary: (imageId: string) => void;
  onDeleteRequest: (image: RecipeImage) => void;
}

export const ImageCard: React.FC<ImageCardProps> = ({
  image,
  index,
  disabled = false,
  onSetPrimary,
  onDeleteRequest,
}) => {
  return (
    <div
      data-testid={`image-card-${index}`}
      className="group flex flex-col rounded-xl bg-white border border-gray-200 overflow-hidden transition-all hover:border-gray-300 hover:shadow-xs"
    >
      {/* Thumbnail Area */}
      <div className="relative aspect-video w-full bg-gray-100 overflow-hidden">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={image.thumbnailUrl || image.mediumUrl || image.originalUrl}
          alt={image.altText || `Ảnh công thức ${index + 1}`}
          className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-102"
          onError={(e) => {
            // Khi ảnh lỗi không tải được, hiển thị fallback nhẹ
            (e.currentTarget as HTMLImageElement).src =
              'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 24 24" fill="none" stroke="%239ca3af" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><path d="M21 15l-5-5L5 21"/></svg>';
          }}
        />

        {/* Primary Badge */}
        {image.isPrimary && (
          <div
            data-testid={`badge-primary-${index}`}
            className="absolute top-2.5 left-2.5 inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-semibold bg-green-800 text-white shadow-xs"
          >
            <svg className="w-3.5 h-3.5 text-green-200" fill="currentColor" viewBox="0 0 20 20">
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
            </svg>
            Ảnh đại diện
          </div>
        )}

        {/* Order Badge */}
        <div className="absolute top-2.5 right-2.5 px-2 py-0.5 rounded-md text-xs font-mono font-medium bg-black/60 text-white backdrop-blur-xs">
          Thứ tự: {image.orderIndex ?? index}
        </div>
      </div>

      {/* Content Area */}
      <div className="p-3.5 flex-1 flex flex-col justify-between gap-3">
        <div className="space-y-1">
          <p
            data-testid={`image-alt-${index}`}
            className="text-xs text-gray-700 line-clamp-2"
            title={image.altText || 'Không có chú thích'}
          >
            {image.altText ? (
              <span>
                <strong className="font-medium text-gray-900">Chú thích:</strong> {image.altText}
              </span>
            ) : (
              <span className="italic text-gray-400">Chưa có chú thích (Alt text)</span>
            )}
          </p>
          <p
            className="text-[11px] text-gray-400 truncate"
            title={image.originalUrl}
          >
            {image.originalUrl}
          </p>
        </div>

        {/* Actions */}
        <div className="flex items-center justify-between gap-2 pt-2 border-t border-gray-100">
          {!image.isPrimary ? (
            <button
              type="button"
              data-testid={`btn-set-primary-${index}`}
              disabled={disabled}
              onClick={() => onSetPrimary(image.id)}
              className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium rounded-md text-green-800 bg-green-50 hover:bg-green-100 border border-green-200 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <svg className="w-3.5 h-3.5 text-green-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
              Đặt làm ảnh chính
            </button>
          ) : (
            <span className="text-xs font-medium text-green-700 flex items-center gap-1">
              <svg className="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
              </svg>
              Đang là ảnh chính
            </span>
          )}

          <button
            type="button"
            data-testid={`btn-delete-image-${index}`}
            disabled={disabled}
            onClick={() => onDeleteRequest(image)}
            className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium rounded-md text-gray-500 hover:text-red-600 bg-white hover:bg-red-50 border border-gray-200 hover:border-red-200 disabled:opacity-40 disabled:cursor-not-allowed transition-colors ml-auto"
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
};
