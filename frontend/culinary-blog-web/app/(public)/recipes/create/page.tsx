'use client';

import { FormEvent, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, ChefHat, Save } from 'lucide-react';
import Link from 'next/link';
import { getCategories } from '@/lib/api/categories';
import { createRecipe } from '@/lib/api/recipes';
import { DIFFICULTY_MAP } from '@/types/recipe';
import { ApiError } from '@/lib/api/client';
import { StepEditor } from '@/components/recipe-management/StepEditor';
import type { Category } from '@/types/category';
import type { RecipeStep } from '@/types/step';

type IngredientDraft = {
  id: string;
  name: string;
  quantity: string;
  unit: string;
  notes: string;
  sortOrder: number;
};

const emptyIngredient = (sortOrder: number): IngredientDraft => ({
  id: crypto.randomUUID(),
  name: '',
  quantity: '1',
  unit: '',
  notes: '',
  sortOrder,
});

export default function CreateRecipePage() {
  const router = useRouter();
  const [categories, setCategories] = useState<Category[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [content, setContent] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [prepTime, setPrepTime] = useState(15);
  const [cookTime, setCookTime] = useState(30);
  const [servings, setServings] = useState(1);
  const [difficulty, setDifficulty] = useState<'Easy' | 'Medium' | 'Hard' | 'Expert'>('Easy');
  const [ingredients, setIngredients] = useState<IngredientDraft[]>([emptyIngredient(0)]);
  const [steps, setSteps] = useState<RecipeStep[]>([]);
  const [nutrition, setNutrition] = useState({
    calories: 0,
    protein: 0,
    carbohydrates: 0,
    fat: 0,
    fiber: 0,
    sodium: 0,
  });

  useEffect(() => {
    getCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  const updateIngredient = (id: string, field: keyof Omit<IngredientDraft, 'id' | 'sortOrder'>, value: string) => {
    setIngredients((current) => current.map((item) => item.id === id ? { ...item, [field]: value } : item));
  };

  const removeIngredient = (id: string) => {
    setIngredients((current) => current.filter((item) => item.id !== id).map((item, index) => ({ ...item, sortOrder: index })));
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');

    if (!title.trim() || !content.trim() || !categoryId) {
      setError('Vui lòng nhập tiêu đề, nội dung và chọn danh mục.');
      return;
    }
    if (!ingredients.some((item) => item.name.trim())) {
      setError('Công thức Draft cần ít nhất một nguyên liệu hợp lệ.');
      return;
    }
    if (!steps.some((item) => item.description.trim())) {
      setError('Công thức cần ít nhất một bước chế biến.');
      return;
    }

    const validIngredients = ingredients.filter((item) => item.name.trim());
    const quantityPattern = /^[-+]?(?:\d+(?:\.\d*)?|\.\d+)$/;
    if (validIngredients.some((item) => !quantityPattern.test(item.quantity.trim()))) {
      setError('Quantity nguyên liệu phải là số thập phân, không dùng chuỗi phân số như 1/2.');
      return;
    }

    setSaving(true);
    try {
      const result = await createRecipe({
        categoryId,
        title: title.trim(),
        description: description.trim() || null,
        content: content.trim(),
        prepTimeMinutes: prepTime,
        cookTimeMinutes: cookTime,
        servings,
        difficulty,
        nutrition,
        ingredients: validIngredients.map((item) => ({
          name: item.name.trim(),
          quantity: Number(item.quantity),
          unit: item.unit.trim() || null,
          notes: item.notes.trim() || null,
          sortOrder: item.sortOrder,
        })),
        steps: steps.map((step, index) => ({
          title: step.title?.trim() || null,
          description: step.description.trim(),
          timerMinutes: step.timerMinutes ?? null,
          imageUrl: step.imageUrl?.trim() || null,
          stepNumber: index + 1,
        })),
        images: [],
      });
      router.push(`/recipes/${result.slug}`);
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError('409 Conflict: dữ liệu công thức đã thay đổi. Vui lòng tải lại và thử lại.');
      } else {
        setError(err instanceof Error ? err.message : 'Không thể tạo công thức.');
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <main className="mx-auto max-w-5xl px-4 py-8">
      <Link href="/recipes" className="mb-5 inline-flex items-center gap-2 text-sm font-semibold text-emerald-700">
        <ArrowLeft className="h-4 w-4" /> Danh sách công thức
      </Link>
      <form onSubmit={submit} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm md:p-8">
        <div className="mb-6 flex items-center gap-3">
          <div className="rounded-xl bg-emerald-100 p-3 text-emerald-700"><ChefHat className="h-6 w-6" /></div>
          <div>
            <p className="text-sm text-slate-500">Trạng thái: Draft</p>
            <h1 className="text-2xl font-bold text-slate-950">Tạo công thức mới</h1>
          </div>
        </div>

        {error && <div role="alert" className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>}

        <section className="grid gap-4 md:grid-cols-2">
          <label className="md:col-span-2">Tiêu đề<input className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} required /></label>
          <label className="md:col-span-2">Mô tả tóm tắt<textarea className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={description} onChange={(e) => setDescription(e.target.value)} /></label>
          <label className="md:col-span-2">Nội dung<textarea className="mt-1 min-h-32 w-full rounded-xl border border-slate-300 px-3 py-2" value={content} onChange={(e) => setContent(e.target.value)} required /></label>
          <label>Danh mục<select className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={categoryId} onChange={(e) => setCategoryId(e.target.value)} required><option value="">-- Chọn danh mục --</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
          <label>Độ khó<select className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={difficulty} onChange={(e) => setDifficulty(e.target.value as typeof difficulty)}>{Object.keys(DIFFICULTY_MAP).map((value) => <option key={value} value={value}>{value}</option>)}</select></label>
          <label>Chuẩn bị (phút)<input type="number" min={0} className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={prepTime} onChange={(e) => setPrepTime(Number(e.target.value))} /></label>
          <label>Nấu (phút)<input type="number" min={0} className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={cookTime} onChange={(e) => setCookTime(Number(e.target.value))} /></label>
          <label>Khẩu phần<input type="number" min={1} className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={servings} onChange={(e) => setServings(Number(e.target.value))} /></label>
        </section>

        <section className="mt-8">
          <h2 className="mb-3 text-xl font-bold">Nguyên liệu</h2>
          <div className="space-y-3">
            {ingredients.map((item, index) => (
              <div key={item.id} className="grid gap-2 rounded-xl border border-slate-200 p-3 md:grid-cols-[28px_1.4fr_.55fr_.55fr_1fr_auto]">
                <span className="pt-2 text-center font-semibold">{index + 1}</span>
                <input aria-label="Tên nguyên liệu" placeholder="Tên nguyên liệu" className="rounded-lg border border-slate-300 px-3 py-2" value={item.name} onChange={(e) => updateIngredient(item.id, 'name', e.target.value)} />
                <input aria-label="Số lượng" placeholder="Số lượng" className="rounded-lg border border-slate-300 px-3 py-2" value={item.quantity} onChange={(e) => updateIngredient(item.id, 'quantity', e.target.value)} />
                <input aria-label="Đơn vị" placeholder="Đơn vị" className="rounded-lg border border-slate-300 px-3 py-2" value={item.unit} onChange={(e) => updateIngredient(item.id, 'unit', e.target.value)} />
                <input aria-label="Ghi chú" placeholder="Ghi chú" className="rounded-lg border border-slate-300 px-3 py-2" value={item.notes} onChange={(e) => updateIngredient(item.id, 'notes', e.target.value)} />
                <button type="button" onClick={() => removeIngredient(item.id)} className="rounded-lg px-3 py-2 text-red-600 hover:bg-red-50">Xóa</button>
              </div>
            ))}
          </div>
          <button type="button" onClick={() => setIngredients((current) => [...current, emptyIngredient(current.length)])} className="mt-3 rounded-lg border border-emerald-300 px-4 py-2 text-sm font-semibold text-emerald-700">+ Thêm nguyên liệu</button>
        </section>

        <section className="mt-8">
          <h2 className="mb-3 text-xl font-bold">Dinh dưỡng</h2>
          <div className="grid gap-3 md:grid-cols-3">
            {([
              ['calories', 'Calories (kcal)'], ['protein', 'Protein (g)'], ['carbohydrates', 'Carbohydrates (g)'],
              ['fat', 'Fat (g)'], ['fiber', 'Fiber (g)'], ['sodium', 'Sodium (mg)'],
            ] as const).map(([field, label]) => (
              <label key={field}>{label}<input type="number" min={0} step="0.01" className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2" value={nutrition[field]} onChange={(e) => setNutrition((current) => ({ ...current, [field]: Number(e.target.value) }))} /></label>
            ))}
          </div>
        </section>

        <section className="mt-8">
          <h2 className="mb-3 text-xl font-bold">Các bước thực hiện</h2>
          <StepEditor initialSteps={steps} onStepsChange={setSteps} disabled={saving} />
        </section>

        <div className="mt-8 flex justify-end gap-3">
          <Link href="/recipes" className="rounded-xl border border-slate-300 px-5 py-2.5 text-sm font-semibold text-slate-700">Hủy</Link>
          <button type="submit" disabled={saving} className="inline-flex items-center gap-2 rounded-xl bg-emerald-700 px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-60"><Save className="h-4 w-4" />{saving ? 'Đang lưu...' : 'Lưu nháp'}</button>
        </div>
      </form>
    </main>
  );
}
