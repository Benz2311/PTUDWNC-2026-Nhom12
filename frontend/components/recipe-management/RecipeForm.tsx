'use client';

import { FormEvent, useEffect, useState } from 'react';
import { apiFetch, apiJson } from '@/lib/api';
import IngredientEditor, { IngredientDraft } from './IngredientEditor';
import StepEditor, { StepDraft } from './StepEditor';
import ImageManager, { RecipeImageDraft } from './ImageManager';

type Category = { id: string; name: string };
type Ingredient = IngredientDraft;
type Step = StepDraft;
type ExistingImage = RecipeImageDraft;
type UploadedImage = { url: string; originalName: string };

export type InitialRecipe = {
  id: string;
  title: string;
  description: string;
  content: string;
  categoryId: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: string;
  ingredients: { name: string; quantity: number | null; unit: string | null; notes?: string | null }[];
  steps: { title: string; description: string }[];
  nutrition: {
    calories: number | null;
    protein: number | null;
    carbohydrates: number | null;
    fat: number | null;
    fiber: number | null;
  } | null;
  images: { url: string; altText: string | null }[];
};

export default function RecipeForm({ recipeId, initialRecipe, etag }: { recipeId?: string; initialRecipe?: InitialRecipe; etag?: string }) {
  const [categories, setCategories] = useState<Category[]>([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [content, setContent] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [cookTimeMinutes, setCookTimeMinutes] = useState('30');
  const [prepTimeMinutes, setPrepTimeMinutes] = useState('1');
  const [servings, setServings] = useState('1');
  const [difficulty, setDifficulty] = useState('Easy');
  const [ingredients, setIngredients] = useState<Ingredient[]>([{ name: '', quantity: '', unit: '', notes: '' }]);
  const [steps, setSteps] = useState<Step[]>([{ title: '', description: '' }]);
  const [imageFiles, setImageFiles] = useState<File[]>([]);
  const [initialImages, setInitialImages] = useState<ExistingImage[]>([]);
  const [nutrition, setNutrition] = useState({ calories: '0', protein: '0', carbohydrates: '0', fat: '0', fiber: '0' });
  const [message, setMessage] = useState('');
  const [saving, setSaving] = useState(false);

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
    setDescription(initialRecipe.description);
    setContent(initialRecipe.content);
    setCategoryId(initialRecipe.categoryId);
    setPrepTimeMinutes(String(initialRecipe.prepTimeMinutes));
    setCookTimeMinutes(String(initialRecipe.cookTimeMinutes));
    setServings(String(initialRecipe.servings));
    setDifficulty(initialRecipe.difficulty);
    setIngredients(initialRecipe.ingredients.length > 0
      ? initialRecipe.ingredients.map((ingredient) => ({
        name: ingredient.name,
        quantity: ingredient.quantity?.toString() ?? '',
        unit: ingredient.unit ?? '',
        notes: ingredient.notes ?? '',
      }))
      : [{ name: '', quantity: '', unit: '', notes: '' }]);
    setSteps(initialRecipe.steps.length > 0 ? initialRecipe.steps : [{ title: '', description: '' }]);
    setInitialImages(initialRecipe.images.map((image) => ({ url: image.url, altText: image.altText ?? initialRecipe.title })));

    if (initialRecipe.nutrition) {
      setNutrition(Object.fromEntries(
        Object.entries(initialRecipe.nutrition).map(([key, value]) => [key, String(value ?? 0)]),
      ) as typeof nutrition);
    }
  }, [initialRecipe]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setMessage('');

    if (recipeId && !etag) {
      setMessage('Không nhận được phiên bản công thức. Hãy tải lại trang trước khi lưu.');
      setSaving(false);
      return;
    }

    if (imageFiles.some((file) => file.size > 5 * 1024 * 1024)) {
      setMessage('Mỗi ảnh không được vượt quá 5 MB.');
      setSaving(false);
      return;
    }

    try {
      let uploadedImages: ExistingImage[] = [];
      if (imageFiles.length > 0) {
        const uploadData = new FormData();
        imageFiles.forEach((file) => uploadData.append('files', file));
        const uploaded = await apiFetch<UploadedImage[]>('/api/v1/files/images', {
          method: 'POST',
          body: uploadData,
        });
        uploadedImages = uploaded.map((image) => ({ url: image.url, altText: title }));
      }

      const saved = await apiFetch<{ id: string; slug: string }>(
        recipeId ? `/api/v1/recipes/${recipeId}` : '/api/v1/recipes',
        {
          method: recipeId ? 'PUT' : 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(recipeId && etag ? { 'If-Match': etag } : {}),
          },
          body: JSON.stringify({
            categoryId,
            title,
            description,
            content,
            prepTimeMinutes: Number(prepTimeMinutes),
            cookTimeMinutes: Number(cookTimeMinutes),
            servings: Number(servings),
            difficulty,
            nutrition: Object.fromEntries(Object.entries(nutrition).map(([key, value]) => [key, Number(value)])),
            ingredients: ingredients
              .filter((ingredient) => ingredient.name.trim())
              .map((ingredient, index) => ({
                name: ingredient.name,
                quantity: ingredient.quantity ? Number(ingredient.quantity) : null,
                unit: ingredient.unit || null,
                notes: ingredient.notes || null,
                sortOrder: index,
              })),
            images: [...initialImages, ...uploadedImages],
            steps: steps.filter((step) => step.description.trim()),
          }),
        },
      );

      window.location.href = `/recipes/${saved.slug}`;
    } catch (error) {
      if ((error as { status?: number }).status === 403) {
        setMessage('Bạn không có quyền sửa công thức này. Chỉ tác giả hoặc quản trị viên mới được cập nhật.');
      } else {
        setMessage(error instanceof Error ? error.message : 'Không thể lưu công thức.');
      }
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
          <label className="field field-wide"><span>Tên công thức</span><input required minLength={5} maxLength={200} value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Ví dụ: Phở bò truyền thống" /></label>
          <label className="field"><span>Danh mục</span><select required value={categoryId} onChange={(event) => setCategoryId(event.target.value)}><option value="" disabled>Chọn danh mục</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
          <label className="field"><span>Thời gian chuẩn bị (phút)</span><input required min="1" type="number" value={prepTimeMinutes} onChange={(event) => setPrepTimeMinutes(event.target.value)} /></label>
          <label className="field"><span>Thời gian nấu (phút)</span><input required min="0" type="number" value={cookTimeMinutes} onChange={(event) => setCookTimeMinutes(event.target.value)} /></label>
          <label className="field"><span>Khẩu phần</span><input required min="1" type="number" value={servings} onChange={(event) => setServings(event.target.value)} /></label>
          <label className="field"><span>Độ khó</span><select value={difficulty} onChange={(event) => setDifficulty(event.target.value)}><option>Easy</option><option>Medium</option><option>Hard</option><option>Expert</option></select></label>
          <label className="field field-wide"><span>Mô tả ngắn</span><input maxLength={2000} value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Một câu giới thiệu hấp dẫn" /></label>
          <label className="field field-wide"><span>Cách thực hiện tổng quát</span><textarea required rows={5} value={content} onChange={(event) => setContent(event.target.value)} placeholder="Mô tả tổng quan cách nấu món ăn..." /></label>
        </div>
      </div>

      <div className="form-section">
        <div className="form-section-heading">
          <span className="step-number">02</span>
          <div><h2>Nguyên liệu</h2><p>Liệt kê nguyên liệu theo thứ tự sử dụng; có thể lưu nháp khi danh sách chưa hoàn chỉnh.</p></div>
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
              <span>{key === 'carbohydrates' ? 'Carbohydrates' : key[0].toUpperCase() + key.slice(1)}</span>
              <input min="0" step="0.01" type="number" value={value} onChange={(event) => setNutrition((current) => ({ ...current, [key]: event.target.value }))} />
            </label>
          ))}
        </div>
      </div>

      <div className="form-section">
        <div className="form-section-heading"><span className="step-number">05</span><div><h2>Ảnh món ăn</h2><p>Chọn nhiều ảnh, tối đa 5 MB mỗi ảnh.</p></div></div>
        <ImageManager images={initialImages} onImagesChange={setInitialImages} onFilesChange={setImageFiles} />
      </div>

      {message && <p className="form-error" role="alert">{message}</p>}
      <div className="form-actions">
        <a className="outline-button" href="/recipes">Hủy</a>
        <button className="primary-button" disabled={saving || !categoryId} type="submit">{saving ? 'Đang lưu...' : 'Lưu bản nháp'}</button>
      </div>
    </form>
  );
}
