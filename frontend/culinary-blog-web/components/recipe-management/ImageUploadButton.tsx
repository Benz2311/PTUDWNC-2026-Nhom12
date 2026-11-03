'use client';

import React, { useRef, useState } from 'react';
import { validateImageFile } from '@/lib/image-validator';

export interface ImageUploadButtonProps {
  onFileSelect: (file: File) => void;
  isUploading?: boolean;
  uploadProgress?: number;
  disabled?: boolean;
  className?: string;
}

export function ImageUploadButton({
  onFileSelect,
  isUploading = false,
  uploadProgress = 0,
  disabled = false,
  className = '',
}: ImageUploadButtonProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleClick = () => {
    if (disabled || isUploading) return;
    setValidationError(null);
    fileInputRef.current?.click();
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Client-side validation: Max 5MB, format: JPG, PNG, WebP, AVIF
    const result = validateImageFile(file);
    if (!result.isValid) {
      setValidationError(result.errorMessage || 'Tệp hình ảnh không hợp lệ.');
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      return;
    }

    setValidationError(null);
    onFileSelect(file);

    // Reset input
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <div className={`space-y-1.5 ${className}`}>
      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        data-testid="input-image-file"
        accept="image/jpeg,image/png,image/webp,image/avif"
        className="hidden"
        onChange={handleFileChange}
        disabled={disabled || isUploading}
      />

      {/* Button */}
      <button
        type="button"
        data-testid="btn-upload-image"
        onClick={handleClick}
        disabled={disabled || isUploading}
        className="relative inline-flex items-center justify-center gap-2 px-4 py-2.5 text-xs sm:text-sm font-semibold rounded-xl text-white bg-emerald-700 hover:bg-emerald-800 active:bg-emerald-900 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-sm hover:shadow-md overflow-hidden"
      >
        {isUploading ? (
          <>
            <svg className="w-4 h-4 animate-spin shrink-0 text-emerald-200" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            <span>Đang tải lên {uploadProgress > 0 ? `${uploadProgress}%` : '...'}</span>
          </>
        ) : (
          <>
            <svg className="w-4 h-4 shrink-0 text-emerald-100" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
            </svg>
            <span>Tải ảnh lên (Max 5MB)</span>
          </>
        )}

        {/* Progress bar background indicator when uploading */}
        {isUploading && uploadProgress > 0 && (
          <span
            className="absolute bottom-0 left-0 h-1 bg-emerald-400 transition-all duration-200"
            style={{ width: `${uploadProgress}%` }}
          />
        )}
      </button>

      {/* Validation Error Alert */}
      {validationError && (
        <p
          data-testid="image-upload-validation-error"
          className="text-xs text-rose-600 font-medium flex items-center gap-1.5 mt-1.5 p-2 rounded-lg bg-rose-50 border border-rose-100"
        >
          <svg className="w-4 h-4 shrink-0 text-rose-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" strokeWidth="2" />
            <line x1="12" y1="8" x2="12" y2="12" strokeWidth="2" strokeLinecap="round" />
            <line x1="12" y1="16" x2="12.01" y2="16" strokeWidth="2" strokeLinecap="round" />
          </svg>
          {validationError}
        </p>
      )}
    </div>
  );
}

export default ImageUploadButton;
