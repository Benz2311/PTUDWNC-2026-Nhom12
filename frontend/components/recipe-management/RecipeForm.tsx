'use client';

import { FormEvent, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch, apiFetchWithResponse, apiJson } from '@/lib/api';
import ImageManager, { RecipeImageDraft } from './ImageManager';
import IngredientEditor, { IngredientDraft } from './IngredientEditor';
import RecipeActions, { RecipeLifecycleAction } from './RecipeActions';
import StepEditor, { StepDraft } from './StepEditor';
import {
  difficultyLevelSchema,
  recipeDraftRequestSchema,
  type DifficultyLevel,
  type RecipeDetail,
  type RecipeStatus,
} from '@/types/recipe';

type Category = { id: string; name: string };
type UploadedImage = { url: string; originalName: string };

export type InitialRecipe = RecipeDetail;

export default function RecipeForm({ recipeId, initialRecipe, initialEtag = '' }: { recipeId?: string; initialRecipe?: InitialRecipe; initialEtag?: string }) {
  const router = useRouter();
  const [categories, setCategories] = useState<Category[]>([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [content, setContent] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [cookTimeMinutes, setCookTimeMinutes] = useState('30');
  const [prepTimeMinutes, setPrepTimeMinutes] = useState('10');
  const [servings, setServings] = useState('2');
  const [difficulty, setDifficulty] = useState<DifficultyLevel>('Easy');
  const [ingredients, setIngredients] = useState<IngredientDraft[]>([{ name: '', quantity: '', unit: '', notes: '' }]);
  const [steps, setSteps] = useState<StepDraft[]>([{ title: '', description: '' }]);
  const [imageFiles, setImageFiles] = useState<File[]>([]);
  const [images, setImages] = useState<RecipeImageDraft[]>([]);
  const [nutrition, setNutrition] = useState({ calories: '0', protein: '0', carbohydrates: '0', fat: '0', fiber: '0', sodium: '0' });
  const [status, setStatus] = useState<RecipeStatus>(initialRecipe?.status ?? 'Draft');
  const [etag, setEtag] = useState(initialEtag);
  const [message, setMessage] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setEtag(initialEtag);
  }, [initialEtag]);

  useEffect(() => {
    let cancelled = false;
    apiJson<Category[]>('/api/v1/categories')
      .then((items) => {
        if (cancelled) return;
        setCategories(items);
        setCategoryId((current) => current || initialRecipe?.categoryId || items[0]?.id || '');
      })
      .catch((error: Error) => {
        if (!cancelled) setMessage(error.message || 'Không tải được danh mục.');
      });

    return () => { cancelled = true; };
  }, [initialRecipe?.categoryId]);

  useEffect(() => {
    if (!initialRecipe) return;

    setTitle(initialRecipe.title);
    setStatus(initialRecipe.status);
    setDescription(initialRecipe.description ?? '');
    setContent(initialRecipe.content);
    setCategoryId(initialRecipe.categoryId);
    setPrepTimeMinutes(String(initialRecipe.prepTimeMinutes));
    setCookTimeMinutes(String(initialRecipe.cookTimeMinutes));
    setServings(String(initialRecipe.servings));
    setDifficulty(initialRecipe.difficulty);
    setIngredients(
      initialRecipe.ingredients.length > 0
        ? [...initialRecipe.ingredients].sort((left, right) => left.sortOrder - right.sortOrder).map((ingredient) => ({
            name: ingredient.name,
            quantity: ingredient.quantity?.toString() ?? '',
            unit: ingredient.unit ?? '',
            notes: ingredient.notes ?? '',
          }))
        : [{ name: '', quantity: '', unit: '', notes: '' }],
    );
    setSteps(initialRecipe.steps.length > 0 ? initialRecipe.steps : [{ title: '', description: '' }]);
    setImages(initialRecipe.images.map((image) => ({ url: image.url, altText: image.altText ?? initialRecipe.title })));

    if (initialRecipe.nutrition) {
      setNutrition(
        Object.fromEntries(
          Object.entries(initialRecipe.nutrition).map(([key, value]) => [key, String(value ?? 0)]),
        ) as typeof nutrition,
      );
    }
  }, [initialRecipe]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setMessage('');

    if (imageFiles.some((file) => file.size > 5 * 1024 * 1024)) {
      setMessage('Mỗi ảnh không được vượt quá 5 MB.');
      setSaving(false);
      return;
    }

    try {
      let uploadedImages: RecipeImageDraft[] = [];
      if (imageFiles.length > 0) {
        const uploadData = new FormData();
        imageFiles.forEach((file) => uploadData.append('files', file));
        const uploaded = await apiFetch<UploadedImage[]>('/api/v1/files/images', {
          method: 'POST',
          body: uploadData,
        });
        uploadedImages = uploaded.map((image) => ({ url: image.url, altText: title }));
      }

      const METHOD = recipeId ? 'PUT' : 'POST';
      const endpoint = recipeId ? `/api/v1/recipes/${recipeId}` : '/api/v1/recipes';
      if (recipeId && !etag) {
        throw new Error('Không nhận được phiên bản công thức. Hãy tải lại trang trước khi lưu.');
      }

      const requestPayload = {
        categoryId,
        title,
        description,
        content,
        prepTimeMinutes: Number(prepTimeMinutes),
        cookTimeMinutes: Number(cookTimeMinutes),
        servings: Number(servings),
        difficulty,
        nutrition: Object.fromEntries(Object.entries(nutrition).map(([key, value]) => [key, Number(value)])),
        ingredients: ingredients.filter((ingredient) => ingredient.name.trim()).map((ingredient, orderIndex) => ({
          name: ingredient.name.trim(),
          quantity: ingredient.quantity ? Number(ingredient.quantity) : null,
          unit: ingredient.unit.trim() || null,
          notes: ingredient.notes.trim() || null,
          orderIndex,
        })),
        images: [...images, ...uploadedImages],
        steps: steps.filter((step) => step.description.trim()),
      };
      const validation = recipeDraftRequestSchema.safeParse(requestPayload);
      if (!validation.success) {
        throw new Error('Dữ liệu công thức chưa hợp lệ. Vui lòng kiểm tra các trường và thử lại.');
      }

      const response = await apiFetchWithResponse<{ id: string; slug: string }>(endpoint, {
        method: METHOD,
        headers: {
          'Content-Type': 'application/json',
          ...(recipeId ? { 'If-Match': etag } : {}),
        },
        body: JSON.stringify(validation.data),
      });

      setEtag(response.headers.get('ETag') ?? etag);
      router.push(`/recipes/${response.data.slug}`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Không thể lưu công thức.');
    } finally {
      setSaving(false);
    }
  }

  async function changeStatus(action: RecipeLifecycleAction) {
    if (!recipeId || !etag) {
      setMessage('Không nhận được phiên bản công thức. Hãy tải lại trang trước khi thực hiện.');
      return;
    }

    if (action === 'unpublish' && !window.confirm('Hủy xuất bản công thức này?')) return;

    setSaving(true);
    setMessage('');
    try {
      const { data, headers } = await apiFetchWithResponse<{ id: string; status: RecipeStatus }>(
        `/api/v1/recipes/${recipeId}/${action}`,
        { method: 'POST', headers: { 'If-Match': etag } },
      );
      setStatus(data.status);
      setEtag(headers.get('ETag') ?? etag);
      setMessage(`Đã cập nhật trạng thái: ${data.status}.`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Không thể cập nhật trạng thái.');
    } finally {
      setSaving(false);
    }
  }

  async function deleteRecipe() {
    if (!recipeId || !etag) return;
    if (!window.confirm('Chuyển công thức vào thùng rác?')) return;

    setSaving(true);
    setMessage('');
    try {
      await apiFetch(`/api/v1/recipes/${recipeId}`, {
        method: 'DELETE',
        headers: { 'If-Match': etag },
      });
      router.replace('/recipes');
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Không thể xóa công thức.');
      setSaving(false);
    }
  }

  return (
    <form className="recipe-form" onSubmit={submit}>
      <div className="form-section">
        <div className="form-section-heading">
          <span className="step-number">01</span>
          <div><h2>Thông tin món ăn</h2><p>Đặt nền tảng cho công thức của bạn.</p></div>
        </div>
        <div className="form-grid">
          <label className="field field-wide">
            <span>Tên công thức</span>
            <input required value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Ví dụ: Phở bò truyền thống" />
          </label>
          <label className="field">
            <span>Danh mục</span>
            <select required value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
              <option value="" disabled>Chọn danh mục</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>{category.name}</option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Thời gian chuẩn bị (phút)</span>
            <input required min="1" type="number" value={prepTimeMinutes} onChange={(event) => setPrepTimeMinutes(event.target.value)} />
          </label>
          <label className="field">
            <span>Thời gian nấu (phút)</span>
            <input required min="0" type="number" value={cookTimeMinutes} onChange={(event) => setCookTimeMinutes(event.target.value)} />
          </label>
          <label className="field">
            <span>Khẩu phần</span>
            <input required min="1" type="number" value={servings} onChange={(event) => setServings(event.target.value)} />
          </label>
          <label className="field">
            <span>Độ khó</span>
            <select
              value={difficulty}
              onChange={(event) => {
                const result = difficultyLevelSchema.safeParse(event.target.value);
                if (result.success) setDifficulty(result.data);
              }}
            >
              <option>Easy</option>
              <option>Medium</option>
              <option>Hard</option>
              <option>Expert</option>
            </select>
          </label>
          <label className="field field-wide">
            <span>Mô tả ngắn</span>
            <input value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Một câu giới thiệu hấp dẫn" />
          </label>
          <label className="field field-wide">
            <span>Cách thực hiện tổng quát</span>
            <textarea required rows={5} value={content} onChange={(event) => setContent(event.target.value)} placeholder="Mô tả tổng quan cách nấu món ăn..." />
          </label>
        </div>
      </div>

      <div className="form-section">
        <div className="form-section-heading">
          <span className="step-number">02</span>
          <div><h2>Nguyên liệu</h2><p>Liệt kê nguyên liệu theo thứ tự sử dụng.</p></div>
        </div>
        <IngredientEditor ingredients={ingredients} onChange={setIngredients} />
      </div>

      <div className="form-section">
        <div className="form-section-heading">
          <span className="step-number">03</span>
          <div><h2>Các bước thực hiện</h2><p>Thêm ít nhất một bước để có thể xuất bản công thức.</p></div>
        </div>
        <StepEditor steps={steps} onChange={setSteps} />
      </div>

      <div className="form-section">
        <div className="form-section-heading"><span className="step-number">04</span><div><h2>Dinh dưỡng</h2><p>Giá trị tham khảo cho một khẩu phần.</p></div></div>
        <div className="nutrition-grid">
          {Object.entries(nutrition).map(([key, value]) => (
            <label className="field" key={key}>
              <span>{key === 'carbohydrates' ? 'Carbohydrates' : key === 'sodium' ? 'Sodium (mg)' : key[0].toUpperCase() + key.slice(1)}</span>
              <input min="0" step="0.01" type="number" value={value} onChange={(event) => setNutrition((current) => ({ ...current, [key]: event.target.value }))} />
            </label>
          ))}
        </div>
      </div>

      <div className="form-section">
        <div className="form-section-heading"><span className="step-number">05</span><div><h2>Ảnh món ăn</h2><p>Chọn nhiều ảnh, tối đa 5 MB mỗi ảnh.</p></div></div>
        <ImageManager images={images} onImagesChange={setImages} onFilesChange={setImageFiles} />
      </div>

      {message && <p className="form-error" role="alert">{message}</p>}
      {recipeId && <RecipeActions status={status} disabled={saving} onAction={changeStatus} onDelete={deleteRecipe} />}
      <div className="form-actions">
        <a className="outline-button" href="/recipes">Hủy</a>
        <button className="primary-button" disabled={saving || !categoryId || (Boolean(recipeId) && !etag)} type="submit">
          {saving ? 'Đang lưu...' : recipeId ? 'Lưu thay đổi' : 'Lưu bản nháp'}
        </button>
      </div>
    </form>
  );
}
