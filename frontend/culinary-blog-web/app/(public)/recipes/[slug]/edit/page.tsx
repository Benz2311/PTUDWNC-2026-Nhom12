'use client';

import { FormEvent, useCallback, useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowLeft, Archive, Globe2, Save, Trash2 } from 'lucide-react';
import { getRecipeBySlug, updateRecipe, changeRecipeStatus, deleteRecipe } from '@/lib/api/recipes';
import { getCategories } from '@/lib/api/categories';
import { ApiError } from '@/lib/api/client';
import RecipeStatusBadge from '@/components/recipe/RecipeStatusBadge';
import { StepEditor } from '@/components/recipe-management/StepEditor';
import { ImageManager } from '@/components/recipe-management/ImageManager';
import { DIFFICULTY_MAP } from '@/types/recipe';
import type { RecipeDetail, RecipeIngredient, RecipeStatus, DifficultyLevel } from '@/types/recipe';
import type { Category } from '@/types/category';

export default function EditRecipePage() {
  const params = useParams<{ slug: string }>();
  const router = useRouter();
  const slug = typeof params?.slug === 'string' ? decodeURIComponent(params.slug) : '';
  const [recipe, setRecipe] = useState<RecipeDetail | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
  const [categoryId, setCategoryId] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [content, setContent] = useState('');
  const [prepTime, setPrepTime] = useState(0);
  const [cookTime, setCookTime] = useState(0);
  const [servings, setServings] = useState(1);
  const [difficulty, setDifficulty] = useState<DifficultyLevel>('Easy');
  const [ingredients, setIngredients] = useState<RecipeIngredient[]>([]);
  const [nutrition, setNutrition] = useState({ calories: 0, protein: 0, carbohydrates: 0, fat: 0, fiber: 0, sodium: 0 });

  const load = useCallback(async () => {
    if (!slug) return;
    setLoading(true);
    try {
      const [data, categoryList] = await Promise.all([getRecipeBySlug(slug), getCategories()]);
      if (!data) throw new Error('Không tìm thấy công thức.');
      setRecipe(data);
      setHasUnsavedChanges(data.status === 'Draft');
      setCategories(categoryList);
      setCategoryId(data.category?.id || data.categoryId || '');
      setTitle(data.title);
      setDescription(data.description || '');
      setContent(data.content || '');
      setPrepTime(data.prepTimeMinutes ?? data.prepTime ?? 0);
      setCookTime(data.cookTimeMinutes ?? data.cookTime ?? 0);
      setServings(data.servings ?? 1);
      setDifficulty((typeof data.difficulty === 'string' ? data.difficulty : 'Easy') as DifficultyLevel);
      setIngredients([...data.ingredients].sort((a, b) => (a.sortOrder ?? a.orderIndex ?? 0) - (b.sortOrder ?? b.orderIndex ?? 0)));
      setNutrition(data.nutrition ? {
        calories: data.nutrition.calories ?? 0,
        protein: data.nutrition.protein ?? 0,
        carbohydrates: data.nutrition.carbohydrates ?? 0,
        fat: data.nutrition.fat ?? 0,
        fiber: data.nutrition.fiber ?? 0,
        sodium: data.nutrition.sodium ?? 0,
      } : { calories: 0, protein: 0, carbohydrates: 0, fat: 0, fiber: 0, sodium: 0 });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể tải công thức.');
    } finally {
      setLoading(false);
    }
  }, [slug]);

  useEffect(() => { load(); }, [load]);

  const save = async (event: FormEvent) => {
    event.preventDefault();
    if (!recipe) return;
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const result = await updateRecipe(recipe.id, {
        categoryId,
        title: title.trim(),
        description: description.trim() || null,
        content: content.trim(),
        prepTimeMinutes: prepTime,
        cookTimeMinutes: cookTime,
        servings,
        difficulty,
        nutrition,
        ingredients: ingredients.filter((item) => item.name.trim()).map((item, index) => ({
          name: item.name.trim(),
          quantity: item.quantity ?? 0,
          unit: item.unit ?? null,
          notes: item.notes ?? null,
          sortOrder: index,
          orderIndex: index,
        })),
        steps: recipe.steps.map((step, index) => ({
          title: step.title ?? null,
          description: step.description,
          timerMinutes: step.timerMinutes ?? null,
          imageUrl: step.imageUrl ?? null,
          stepNumber: index + 1,
        })),
        images: recipe.images.map((image, index) => ({ url: image.url ?? image.originalUrl, altText: image.altText, isPrimary: image.isPrimary, orderIndex: index })),
      }, recipe.version);
      setHasUnsavedChanges(false);
      setSuccess(recipe.status === 'Published'
        ? 'Đã lưu thay đổi công thức.'
        : 'Đã lưu thay đổi. Công thức vẫn nằm trong Kho lưu trữ cho đến khi bạn chọn xuất bản.');
      setRecipe((current) => current ? {
        ...current,
        categoryId,
        category: { ...current.category, id: categoryId },
        title: title.trim(),
        description: description.trim(),
        content: content.trim(),
        prepTimeMinutes: prepTime,
        cookTimeMinutes: cookTime,
        servings,
        difficulty,
        nutrition,
        ingredients: ingredients.filter((item) => item.name.trim()),
        slug: result.slug,
        version: result.version ?? current.version,
      } : current);
      router.replace(`/recipes/${encodeURIComponent(result.slug)}/edit`);
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError('409 Conflict: công thức đã được cập nhật ở phiên bản khác. Tải lại dữ liệu trước khi lưu.');
      } else {
        setError(err instanceof Error ? err.message : 'Không thể cập nhật công thức.');
      }
    } finally {
      setSaving(false);
    }
  };

  const lifecycle = async (action: 'publish' | 'unpublish' | 'archive' | 'unarchive') => {
    if (!recipe) return;
    if (action === 'publish' && hasUnsavedChanges) {
      setError('Hãy rà soát và lưu bản nháp trước khi xuất bản công thức.');
      return;
    }
    const labels = { publish: 'xuất bản', unpublish: 'hủy xuất bản', archive: 'lưu trữ', unarchive: 'mở lưu trữ' };
    if (!window.confirm(`Xác nhận ${labels[action]} công thức này?`)) return;
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      await changeRecipeStatus(recipe.id, action, recipe.version);
      await load();
      setSuccess(action === 'publish' ? 'Đã xuất bản công thức lên cộng đồng.' : 'Đã cập nhật trạng thái công thức.');
      router.refresh();
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) setError('409 Conflict: phiên bản công thức đã thay đổi.');
      else if (err instanceof ApiError && err.status === 422) setError(err.message || 'Công thức chưa đáp ứng điều kiện xuất bản.');
      else setError(err instanceof Error ? err.message : 'Không thể thay đổi trạng thái.');
    } finally {
      setSaving(false);
    }
  };

  const remove = async () => {
    if (!recipe || !window.confirm('Xóa công thức này vào Thùng rác?')) return;
    setSaving(true);
    try {
      await deleteRecipe(recipe.id, recipe.version);
      router.push('/recipes');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể xóa công thức.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <main className="mx-auto max-w-5xl px-4 py-12">Đang tải công thức...</main>;
  if (!recipe) return <main className="mx-auto max-w-5xl px-4 py-12 text-red-700">{error || 'Không tìm thấy công thức.'}</main>;

  return (
    <main className="mx-auto max-w-6xl px-4 py-8">
      <Link href="/my-recipes?tab=vault" className="mb-5 inline-flex items-center gap-2 text-sm font-semibold text-emerald-700"><ArrowLeft className="h-4 w-4" />Kho công thức của tôi</Link>
      <form onSubmit={save} onChange={() => setHasUnsavedChanges(true)} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm md:p-8">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
          <div><p className="text-sm text-slate-500">Chỉnh sửa công thức</p><h1 className="text-2xl font-bold">{recipe.title}</h1></div>
          <RecipeStatusBadge status={recipe.status as RecipeStatus} />
        </div>
        {error && <div role="alert" className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}
        {success && <div role="status" className="mb-5 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">{success}</div>}
        {(recipe.status === 'Draft' || recipe.status === 'Archived') && hasUnsavedChanges && (
          <p role="status" className="mb-5 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
            Hãy rà soát nội dung và lưu bản nháp trước khi xuất bản.
          </p>
        )}
        <div className="grid gap-4 md:grid-cols-2">
          <label className="md:col-span-2">Tiêu đề<input required maxLength={200} className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={title} onChange={(e) => setTitle(e.target.value)} /></label>
          <label className="md:col-span-2">Mô tả<textarea className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={description} onChange={(e) => setDescription(e.target.value)} /></label>
          <label className="md:col-span-2">Nội dung<textarea required className="mt-1 min-h-32 w-full rounded-xl border border-slate-300 px-3 py-2" value={content} onChange={(e) => setContent(e.target.value)} /></label>
          <label>Danh mục<select required className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={categoryId} onChange={(e) => setCategoryId(e.target.value)}><option value="">-- Chọn danh mục --</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
          <label>Độ khó<select className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={difficulty} onChange={(e) => setDifficulty(e.target.value as DifficultyLevel)}>{Object.keys(DIFFICULTY_MAP).map((value) => <option key={value} value={value}>{value}</option>)}</select></label>
          <label>Chuẩn bị (phút)<input type="number" min={0} className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={prepTime} onChange={(e) => setPrepTime(Number(e.target.value))} /></label>
          <label>Nấu (phút)<input type="number" min={0} className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={cookTime} onChange={(e) => setCookTime(Number(e.target.value))} /></label>
          <label>Khẩu phần<input type="number" min={1} className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={servings} onChange={(e) => setServings(Number(e.target.value))} /></label>
        </div>

        <section className="mt-8">
          <h2 className="mb-3 text-xl font-bold">Nguyên liệu</h2>
          {ingredients.map((item, index) => (
            <div key={item.id} className="mb-2 grid gap-2 md:grid-cols-[28px_1fr_.5fr_.5fr_1fr_auto]">
              <span className="pt-2 text-center">{index + 1}</span>
              <input className="rounded-lg border border-slate-300 px-3 py-2" value={item.name} onChange={(e) => setIngredients((current) => current.map((entry) => entry.id === item.id ? { ...entry, name: e.target.value } : entry))} />
              <input type="number" step="0.001" className="rounded-lg border border-slate-300 px-3 py-2" value={item.quantity ?? ''} onChange={(e) => setIngredients((current) => current.map((entry) => entry.id === item.id ? { ...entry, quantity: Number(e.target.value) } : entry))} />
              <input className="rounded-lg border border-slate-300 px-3 py-2" value={item.unit ?? ''} onChange={(e) => setIngredients((current) => current.map((entry) => entry.id === item.id ? { ...entry, unit: e.target.value } : entry))} />
              <input className="rounded-lg border border-slate-300 px-3 py-2" value={item.notes ?? ''} onChange={(e) => setIngredients((current) => current.map((entry) => entry.id === item.id ? { ...entry, notes: e.target.value } : entry))} />
              <button type="button" className="text-red-600" onClick={() => setIngredients((current) => current.filter((entry) => entry.id !== item.id))}>Xóa</button>
            </div>
          ))}
          <button type="button" className="mt-2 rounded-lg border border-emerald-300 px-4 py-2 text-emerald-700" onClick={() => setIngredients((current) => [...current, { id: crypto.randomUUID(), name: '', quantity: 1, unit: '', notes: '', sortOrder: current.length }])}>+ Thêm nguyên liệu</button>
        </section>

        <section className="mt-8">
          <h2 className="mb-3 text-xl font-bold">Dinh dưỡng</h2>
          <div className="grid gap-3 md:grid-cols-3">
            {([
              ['calories', 'Calories'], ['protein', 'Protein'], ['carbohydrates', 'Carbohydrates'],
              ['fat', 'Fat'], ['fiber', 'Fiber'], ['sodium', 'Sodium'],
            ] as const).map(([field, label]) => <label key={field}>{label}<input type="number" step="0.01" min={0} className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={nutrition[field]} onChange={(e) => setNutrition((current) => ({ ...current, [field]: Number(e.target.value) }))} /></label>)}
          </div>
        </section>

        <section className="mt-8"><h2 className="mb-3 text-xl font-bold">Các bước thực hiện</h2><StepEditor recipeId={recipe.id} initialSteps={recipe.steps} /></section>
        <section className="mt-8"><h2 className="mb-3 text-xl font-bold">Thư viện ảnh</h2><ImageManager recipeId={recipe.id} initialImages={recipe.images} /></section>

        <div className="mt-8 flex flex-wrap justify-end gap-2">
          {(recipe.status === 'Draft' || recipe.status === 'Archived') && <button type="button" disabled={saving || hasUnsavedChanges} onClick={() => lifecycle('publish')} className="inline-flex items-center gap-2 rounded-xl bg-emerald-700 px-4 py-2.5 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:opacity-50"><Globe2 className="h-4 w-4" />{hasUnsavedChanges ? 'Lưu trước khi xuất bản' : 'Xuất bản'}</button>}
          {recipe.status === 'Published' && <><button type="button" disabled={saving} onClick={() => lifecycle('unpublish')} className="rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-semibold">Hủy xuất bản</button><button type="button" disabled={saving} onClick={() => lifecycle('archive')} className="inline-flex items-center gap-2 rounded-xl border border-amber-300 px-4 py-2.5 text-sm font-semibold text-amber-800"><Archive className="h-4 w-4" />Lưu trữ</button></>}
          {recipe.status === 'Archived' && <button type="button" disabled={saving} onClick={() => lifecycle('unarchive')} className="rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-semibold">Mở lưu trữ</button>}
          <button type="submit" disabled={saving} className="inline-flex items-center gap-2 rounded-xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white"><Save className="h-4 w-4" />Lưu thay đổi</button>
          <button type="button" disabled={saving} onClick={remove} className="inline-flex items-center gap-2 rounded-xl border border-red-300 px-4 py-2.5 text-sm font-semibold text-red-700"><Trash2 className="h-4 w-4" />Xóa</button>
        </div>
      </form>
    </main>
  );
}
