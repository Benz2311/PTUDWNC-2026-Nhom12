'use client';

import { useEffect, useState } from 'react';
import Image from 'next/image';

export type RecipeImageDraft = { url: string; altText: string };
type ImageManagerProps = {
  images: RecipeImageDraft[];
  onImagesChange: (images: RecipeImageDraft[]) => void;
  onFilesChange: (files: File[]) => void;
};

export default function ImageManager({ images, onImagesChange, onFilesChange }: ImageManagerProps) {
  const [previews, setPreviews] = useState<{ name: string; url: string }[]>([]);

  useEffect(() => () => previews.forEach((preview) => URL.revokeObjectURL(preview.url)), [previews]);

  function selectFiles(fileList: FileList | null) {
    previews.forEach((preview) => URL.revokeObjectURL(preview.url));
    const files = Array.from(fileList ?? []);
    onFilesChange(files);
    setPreviews(files.map((file) => ({ name: file.name, url: URL.createObjectURL(file) })));
  }

  function updateAltText(index: number, altText: string) {
    onImagesChange(images.map((image, imageIndex) => imageIndex === index ? { ...image, altText } : image));
  }

  function moveImage(index: number, offset: -1 | 1) {
    const target = index + offset;
    if (target < 0 || target >= images.length) return;
    const reordered = [...images];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    onImagesChange(reordered);
  }

  return (
    <>
      <label className="upload-dropzone">
        <input type="file" accept="image/jpeg,image/png,image/webp,image/avif" multiple onChange={(event) => selectFiles(event.target.files)} />
        <strong>Chọn ảnh món ăn</strong><span>JPG, PNG, WebP hoặc AVIF · Tối đa 5 MB/ảnh</span>
      </label>
      {images.length > 0 && (
        <div className="upload-preview-grid">
          {images.map((image, index) => (
            <div className="upload-preview" key={`${image.url}-${index}`}>
              <Image src={image.url} alt={image.altText} width={320} height={320} unoptimized />
              <label className="field">
                <span>Mô tả ảnh {index + 1}</span>
                <input maxLength={300} value={image.altText} onChange={(event) => updateAltText(index, event.target.value)} />
              </label>
              <div className="ingredient-row-actions">
                <button type="button" aria-label="Chuyển ảnh lên" disabled={index === 0} onClick={() => moveImage(index, -1)}>↑</button>
                <button type="button" aria-label="Chuyển ảnh xuống" disabled={index === images.length - 1} onClick={() => moveImage(index, 1)}>↓</button>
              </div>
            </div>
          ))}
          {previews.map((preview) => (
            <div className="upload-preview" key={preview.url}>
              <Image src={preview.url} alt={preview.name} width={320} height={320} unoptimized />
              <span>{preview.name}</span>
            </div>
          ))}
        </div>
      )}
      {images.length === 0 && previews.length > 0 && (
        <div className="upload-preview-grid">
          {previews.map((preview) => (
            <div className="upload-preview" key={preview.url}>
              <Image src={preview.url} alt={preview.name} width={320} height={320} unoptimized />
              <span>{preview.name}</span>
            </div>
          ))}
        </div>
      )}
    </>
  );
}
