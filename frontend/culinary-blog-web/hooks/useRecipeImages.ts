'use client';

import { useState, useCallback, useEffect } from 'react';
import type { RecipeImage, AddRecipeImageInput } from '@/types/image';
import {
  getRecipeImages,
  addRecipeImage as apiAddImage,
  uploadRecipeImageFile as apiUploadImage,
  setPrimaryRecipeImage as apiSetPrimary,
  deleteRecipeImage as apiDeleteImage,
} from '@/lib/api/images';

export interface UseRecipeImagesOptions {
  recipeId?: string;
  initialImages?: RecipeImage[];
  onImagesChange?: (images: RecipeImage[]) => void;
}

export function useRecipeImages({
  recipeId,
  initialImages = [],
  onImagesChange,
}: UseRecipeImagesOptions = {}) {
  const isDirectApi = Boolean(recipeId && recipeId.trim() !== '');

  const [images, setImages] = useState<RecipeImage[]>(() =>
    [...initialImages].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0))
  );
  const [isLoading, setIsLoading] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  const [error, setError] = useState<string | null>(null);
  const [previewImage, setPreviewImage] = useState<RecipeImage | null>(null);

  const primaryImage = images.find((img) => img.isPrimary) || null;

  const updateImagesState = useCallback(
    (newImages: RecipeImage[]) => {
      const sorted = [...newImages].sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
      setImages(sorted);
      onImagesChange?.(sorted);
    },
    [onImagesChange]
  );

  // Load from backend when recipeId is present
  useEffect(() => {
    let cancelled = false;

    if (isDirectApi && initialImages.length === 0) {
      setIsLoading(true);
      setError(null);

      getRecipeImages(recipeId!)
        .then((fetched) => {
          if (!cancelled) {
            updateImagesState(fetched);
          }
        })
        .catch((err) => {
          if (!cancelled) {
            setError(err instanceof Error ? err.message : 'Không tải được danh sách hình ảnh.');
          }
        })
        .finally(() => {
          if (!cancelled) {
            setIsLoading(false);
          }
        });
    }

    return () => {
      cancelled = true;
    };
  }, [isDirectApi, recipeId, initialImages.length, updateImagesState]);

  const uploadImage = useCallback(
    async (file: File, altText?: string, isPrimary?: boolean): Promise<RecipeImage> => {
      setIsUploading(true);
      setUploadProgress(10);
      setError(null);

      try {
        if (isDirectApi && recipeId) {
          const uploaded = await apiUploadImage(
            recipeId,
            file,
            altText,
            isPrimary,
            (progressEvent) => {
              if (progressEvent.total) {
                const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
                setUploadProgress(percent);
              }
            }
          );

          // Update state and handle primary flag replacement
          let next = [...images];
          if (uploaded.isPrimary) {
            next = next.map((img) => ({ ...img, isPrimary: false }));
          }
          next.push(uploaded);
          updateImagesState(next);
          return uploaded;
        }

        // Controlled local mode
        setUploadProgress(50);
        const objectUrl = URL.createObjectURL(file);
        const shouldBePrimary = isPrimary !== undefined ? isPrimary : images.length === 0;
        const nextOrderIndex = images.length > 0 ? Math.max(...images.map((i) => i.orderIndex ?? 0)) + 1 : 0;

        let next = [...images];
        if (shouldBePrimary) {
          next = next.map((img) => ({ ...img, isPrimary: false }));
        }

        const localImage: RecipeImage = {
          id: `temp-img-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
          originalUrl: objectUrl,
          mediumUrl: objectUrl,
          thumbnailUrl: objectUrl,
          altText: altText?.trim() || null,
          isPrimary: shouldBePrimary,
          orderIndex: nextOrderIndex,
          createdAt: new Date().toISOString(),
        };

        setUploadProgress(100);
        next.push(localImage);
        updateImagesState(next);
        return localImage;
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Lỗi khi tải ảnh lên.';
        setError(msg);
        throw err;
      } finally {
        setIsUploading(false);
        setUploadProgress(0);
      }
    },
    [isDirectApi, recipeId, images, updateImagesState]
  );

  const addImageUrl = useCallback(
    async (input: AddRecipeImageInput): Promise<RecipeImage> => {
      setIsLoading(true);
      setError(null);

      try {
        if (isDirectApi && recipeId) {
          const added = await apiAddImage(recipeId, input);
          let next = [...images];
          if (added.isPrimary) {
            next = next.map((img) => ({ ...img, isPrimary: false }));
          }
          next.push(added);
          updateImagesState(next);
          return added;
        }

        // Controlled local mode
        const shouldBePrimary = input.isPrimary !== null && input.isPrimary !== undefined ? input.isPrimary : images.length === 0;
        const nextOrder = input.orderIndex ?? (images.length > 0 ? Math.max(...images.map((i) => i.orderIndex ?? 0)) + 1 : 0);

        let next = [...images];
        if (shouldBePrimary) {
          next = next.map((img) => ({ ...img, isPrimary: false }));
        }

        const newImage: RecipeImage = {
          id: `temp-img-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
          originalUrl: input.originalUrl.trim(),
          mediumUrl: input.mediumUrl?.trim() || null,
          thumbnailUrl: input.thumbnailUrl?.trim() || null,
          altText: input.altText?.trim() || null,
          isPrimary: shouldBePrimary,
          orderIndex: nextOrder,
          createdAt: new Date().toISOString(),
        };

        next.push(newImage);
        updateImagesState(next);
        return newImage;
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Lỗi khi thêm hình ảnh.';
        setError(msg);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [isDirectApi, recipeId, images, updateImagesState]
  );

  const setPrimary = useCallback(
    async (imageId: string): Promise<void> => {
      setIsLoading(true);
      setError(null);

      try {
        if (isDirectApi && recipeId) {
          await apiSetPrimary(recipeId, imageId);
          const next = images.map((img) => ({
            ...img,
            isPrimary: img.id === imageId,
          }));
          updateImagesState(next);
          return;
        }

        // Controlled local mode
        const next = images.map((img) => ({
          ...img,
          isPrimary: img.id === imageId,
        }));
        updateImagesState(next);
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Lỗi khi đặt ảnh chính.';
        setError(msg);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [isDirectApi, recipeId, images, updateImagesState]
  );

  const deleteImage = useCallback(
    async (imageId: string): Promise<void> => {
      setIsDeleting(true);
      setError(null);

      try {
        if (isDirectApi && recipeId) {
          await apiDeleteImage(recipeId, imageId);
          const remaining = images.filter((img) => img.id !== imageId);
          // Backend re-assigns primary if needed:
          const wasPrimary = images.find((i) => i.id === imageId)?.isPrimary;
          if (wasPrimary && remaining.length > 0) {
            remaining.sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
            remaining[0].isPrimary = true;
          }
          updateImagesState(remaining);
          return;
        }

        // Controlled local mode
        const target = images.find((img) => img.id === imageId);
        const remaining = images.filter((img) => img.id !== imageId);
        if (target?.isPrimary && remaining.length > 0) {
          remaining.sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0));
          remaining[0].isPrimary = true;
        }
        updateImagesState(remaining);
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Lỗi khi xóa hình ảnh.';
        setError(msg);
        throw err;
      } finally {
        setIsDeleting(false);
      }
    },
    [isDirectApi, recipeId, images, updateImagesState]
  );

  return {
    images,
    primaryImage,
    isLoading,
    isUploading,
    isDeleting,
    uploadProgress,
    error,
    setError,
    previewImage,
    setPreviewImage,
    uploadImage,
    addImageUrl,
    setPrimary,
    deleteImage,
    setImages: updateImagesState,
  };
}
