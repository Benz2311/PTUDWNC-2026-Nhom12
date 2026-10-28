'use client';

import { FormEvent, useEffect, useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch, apiJson, type ApiError } from '@/lib/api';
import {
  changeRecipeStatus,
  createRecipe,
  deleteRecipe as deleteRecipeRequest,
  updateRecipe,
} from '@/lib/api/recipes';
import ImageManager, { RecipeImageDraft } from './ImageManager';
import IngredientEditor, { IngredientDraft } from './IngredientEditor';
import NutritionEditor, { NutritionData } from './NutritionEditor';
import RecipeActions, { RecipeLifecycleAction } from './RecipeActions';
import StepEditor, { StepDraft } from './StepEditor';
import {
  difficultyLevelSchema,
  recipeDraftRequestSchema,
  type DifficultyLevel,
  type RecipeDetail,
  type RecipeStatus,
} from '@/types/recipe';
import RecipeFormLayout from '@/components/recipes/RecipeFormLayout';

type Category = { id: string; name: string };
type UploadedImage = { url: string; originalName: string };
type UserProfile = { id: string; fullName: string; email: string; emailConfirmed?: boolean };

export type InitialRecipe = RecipeDetail;

type RecipeFormProps = {
  recipeId?: string;
  initialRecipe?: InitialRecipe;
  initialEtag?: string;
  onReload?: () => Promise<void> | void;
};

export default function RecipeForm({
  recipeId,
  initialRecipe,
  initialEtag = '',
  onReload,
}: RecipeFormProps) {
  const router = useRouter();

  // Core metadata fields
  const [categories, setCategories] = useState<Category[]>([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [content, setContent] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [cookTimeMinutes, setCookTimeMinutes] = useState('30');
  const [prepTimeMinutes, setPrepTimeMinutes] = useState('10');
  const [servings, setServings] = useState('2');
  const [difficulty, setDifficulty] = useState<DifficultyLevel>('Easy');

  // Child collections
  const [ingredients, setIngredients] = useState<IngredientDraft[]>([
    { name: '', quantity: '', unit: '', notes: '', orderIndex: 0 },
  ]);
  const [steps, setSteps] = useState<StepDraft[]>([{ title: '', description: '' }]);
  const [imageFiles, setImageFiles] = useState<File[]>([]);
  const [images, setImages] = useState<RecipeImageDraft[]>([]);
  const [nutrition, setNutrition] = useState<NutritionData>({
    calories: '0',
    protein: '0',
    carbohydrates: '0',
    fat: '0',
    fiber: '0',
    sodium: '0',
  });

  // State management & lifecycle
  const [status, setStatus] = useState<RecipeStatus>(initialRecipe?.status ?? 'Draft');
  const [etag, setEtag] = useState(initialEtag);
  const [currentUser, setCurrentUser] = useState<UserProfile | null>(null);

  // UI state
  const [message, setMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [is409Conflict, setIs409Conflict] = useState(false);
  const [saving, setSaving] = useState(false);
  const [isReloading, setIsReloading] = useState(false);

  // Sync ETag whenever prop updates
  useEffect(() => {
    setEtag(initialEtag);
    if (initialEtag) {
      setIs409Conflict(false);
    }
  }, [initialEtag]);

  // Load current user profile to verify email confirmation condition
  useEffect(() => {
    let cancelled = false;
    apiJson<UserProfile>('/api/v1/auth/me')
      .then((user) => {
        if (!cancelled) setCurrentUser(user);
      })
      .catch(() => {
        // User not logged in or endpoint unauthenticated
      });

    return () => {
      cancelled = true;
    };
  }, []);

  // Load category list
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

    return () => {
      cancelled = true;
    };
  }, [initialRecipe?.categoryId]);

  // Populate form when initialRecipe arrives or updates
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

    if (initialRecipe.ingredients.length > 0) {
      const sorted = [...initialRecipe.ingredients].sort(
        (a, b) => (a.sortOrder ?? a.orderIndex ?? 0) - (b.sortOrder ?? b.orderIndex ?? 0),
      );
      setIngredients(
        sorted.map((item, idx) => ({
          id: item.id,
          name: item.name,
          quantity: item.quantity !== null && item.quantity !== undefined ? String(item.quantity) : '',
          unit: item.unit ?? '',
          notes: item.notes ?? '',
          orderIndex: item.orderIndex ?? item.sortOrder ?? idx,
        })),
      );
    } else {
      setIngredients([{ name: '', quantity: '', unit: '', notes: '', orderIndex: 0 }]);
    }

    setSteps(
      initialRecipe.steps.length > 0
        ? initialRecipe.steps.map((s) => ({ title: s.title, description: s.description }))
        : [{ title: '', description: '' }],
    );

    setImages(
      initialRecipe.images.map((image) => ({
        url: image.url,
        altText: image.altText ?? initialRecipe.title,
      })),
    );

    if (initialRecipe.nutrition) {
      setNutrition({
        calories: String(initialRecipe.nutrition.calories ?? 0),
        protein: String(initialRecipe.nutrition.protein ?? 0),
        carbohydrates: String(initialRecipe.nutrition.carbohydrates ?? 0),
        fat: String(initialRecipe.nutrition.fat ?? 0),
        fiber: String(initialRecipe.nutrition.fiber ?? 0),
        sodium: String(initialRecipe.nutrition.sodium ?? 0),
      });
    }
  }, [initialRecipe]);

  // Compute publish eligibility condition
  const publishBlocker = useMemo(() => {
    if (!title.trim() || title.trim().length < 5 || title.trim().length > 200) {
      return 'Tiêu đề công thức phải từ 5 đến 200 ký tự.';
    }
    if (!content.trim()) {
      return 'Nội dung hướng dẫn tổng quát không được để trống.';
    }
    if (!categoryId) {
      return 'Chưa chọn danh mục món ăn.';
    }
    const prep = Number(prepTimeMinutes);
    const cook = Number(cookTimeMinutes);
    const srv = Number(servings);
    if (isNaN(prep) || prep <= 0) {
      return 'Thời gian chuẩn bị phải lớn hơn 0 phút.';
    }
    if (isNaN(cook) || cook < 0) {
      return 'Thời gian nấu không được là số âm.';
    }
    if (isNaN(srv) || srv <= 0) {
      return 'Khẩu phần ăn phải lớn hơn 0.';
    }
    const validIngredients = ingredients.filter((item) => item.name.trim().length > 0);
    if (validIngredients.length === 0) {
      return 'Công thức phải có ít nhất một nguyên liệu.';
    }
    const validSteps = steps.filter((step) => step.description.trim().length > 0);
    if (validSteps.length === 0) {
      return 'Công thức phải có ít nhất một bước thực hiện.';
    }
    if (currentUser && currentUser.emailConfirmed === false) {
      return 'Tài khoản tác giả cần xác thực email trước khi được xuất bản công thức.';
    }
    return null;
  }, [
    title,
    content,
    categoryId,
    prepTimeMinutes,
    cookTimeMinutes,
    servings,
    ingredients,
    steps,
    currentUser,
  ]);

  // Handle reload on 409 Conflict
  async function handleReload() {
    setIsReloading(true);
    setMessage('');
    setIs409Conflict(false);
    try {
      if (onReload) {
        await onReload();
        setSuccessMessage('Đã tải lại dữ liệu mới nhất từ máy chủ.');
      }
    } catch {
      setMessage('Không thể tải lại dữ liệu. Vui lòng làm mới trang (F5).');
    } finally {
      setIsReloading(false);
    }
  }

  // Submit handler (Create Draft or Update Draft/Recipe)
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setMessage('');
    setSuccessMessage('');
    setIs409Conflict(false);

    if (imageFiles.some((file) => file.size > 5 * 1024 * 1024)) {
      setMessage('Mỗi tệp ảnh tải lên không được vượt quá 5 MB.');
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

      if (recipeId && !etag) {
        throw new Error('Không nhận được phiên bản công thức (ETag). Hãy tải lại trang trước khi lưu.');
      }

      const requestPayload = {
        categoryId,
        title: title.trim(),
        description: description.trim(),
        content: content.trim(),
        prepTimeMinutes: Number(prepTimeMinutes),
        cookTimeMinutes: Number(cookTimeMinutes),
        servings: Number(servings),
        difficulty,
        nutrition: {
          calories: Math.max(0, Number(nutrition.calories) || 0),
          protein: Math.max(0, Number(nutrition.protein) || 0),
          carbohydrates: Math.max(0, Number(nutrition.carbohydrates) || 0),
          fat: Math.max(0, Number(nutrition.fat) || 0),
          fiber: Math.max(0, Number(nutrition.fiber) || 0),
          sodium: Math.max(0, Number(nutrition.sodium) || 0),
        },
        ingredients: ingredients
          .filter((ingredient) => ingredient.name.trim())
          .map((ingredient, index) => ({
            name: ingredient.name.trim(),
            quantity: ingredient.quantity ? Number(ingredient.quantity) : null,
            unit: ingredient.unit.trim() || null,
            notes: ingredient.notes.trim() || null,
            orderIndex: ingredient.orderIndex !== undefined ? ingredient.orderIndex : index,
          })),
        images: [...images, ...uploadedImages],
        steps: steps
          .filter((step) => step.description.trim())
          .map((step) => ({
            title: step.title.trim(),
            description: step.description.trim(),
          })),
      };

      const validation = recipeDraftRequestSchema.safeParse(requestPayload);
      if (!validation.success) {
        const firstError = validation.error.issues[0]?.message;
        throw new Error(
          firstError || 'Dữ liệu công thức chưa hợp lệ. Vui lòng kiểm tra lại các trường thông tin.',
        );
      }

      const response = recipeId
        ? await updateRecipe(recipeId, validation.data, etag)
        : await createRecipe(validation.data);

      const newEtag = response.headers.get('ETag') ?? etag;
      setEtag(newEtag);
      setSuccessMessage(
        recipeId ? 'Đã lưu thay đổi công thức thành công!' : 'Đã tạo bản nháp công thức thành công!',
      );

      // Redirect to recipe detail view
      router.push(`/recipes/${response.data.slug}`);
    } catch (error: unknown) {
      const apiErr = error as ApiError;
      if (apiErr.status === 409 || apiErr.message?.includes('409') || apiErr.message?.includes('thay đổi bởi một yêu cầu khác')) {
        setIs409Conflict(true);
        setMessage(
          'Xung đột phiên bản (409 Conflict): Công thức này đã bị thay đổi bởi phiên làm việc khác. Dữ liệu của bạn chưa được lưu. Hãy bấm "Tải lại dữ liệu" để xem phiên bản mới nhất.',
        );
      } else {
        setMessage(apiErr.message || 'Không thể lưu công thức. Vui lòng thử lại.');
      }
    } finally {
      setSaving(false);
    }
  }

  // Change recipe lifecycle state
  async function changeStatus(action: RecipeLifecycleAction) {
    if (!recipeId || !etag) {
      setMessage('Không nhận được phiên bản công thức (ETag). Hãy tải lại trang trước khi thực hiện.');
      return;
    }

    if (action === 'publish' && publishBlocker) {
      setMessage(`Không thể xuất bản: ${publishBlocker}`);
      return;
    }

    setSaving(true);
    setMessage('');
    setSuccessMessage('');
    setIs409Conflict(false);

    try {
      const { data, headers } = await changeRecipeStatus(recipeId, action, etag);
      const newEtag = headers.get('ETag') ?? etag;
      setStatus(data.status);
      setEtag(newEtag);

      const statusMap: Record<RecipeStatus, string> = {
        Draft: 'Bản nháp',
        Published: 'Đã xuất bản',
        Archived: 'Đã lưu trữ',
      };
      setSuccessMessage(`Đã cập nhật trạng thái thành công: ${statusMap[data.status] || data.status}.`);
    } catch (error: unknown) {
      const apiErr = error as ApiError;
      if (apiErr.status === 409 || apiErr.message?.includes('409')) {
        setIs409Conflict(true);
        setMessage(
          'Xung đột phiên bản (409 Conflict): Trạng thái công thức đã bị thay đổi bởi một yêu cầu khác. Hãy bấm "Tải lại dữ liệu".',
        );
      } else {
        setMessage(apiErr.message || 'Không thể cập nhật trạng thái công thức.');
      }
    } finally {
      setSaving(false);
    }
  }

  // Delete recipe
  async function deleteRecipe() {
    if (!recipeId || !etag) return;

    setSaving(true);
    setMessage('');
    setIs409Conflict(false);

    try {
      await deleteRecipeRequest(recipeId, etag);
      router.replace('/recipes');
    } catch (error: unknown) {
      const apiErr = error as ApiError;
      if (apiErr.status === 409 || apiErr.message?.includes('409')) {
        setIs409Conflict(true);
        setMessage(
          'Xung đột phiên bản (409 Conflict): Công thức đã bị chỉnh sửa hoặc đã xóa trước đó.',
        );
      } else {
        setMessage(apiErr.message || 'Không thể xóa công thức.');
      }
      setSaving(false);
    }
  }

  return (
    <form className="recipe-form" onSubmit={submit}>
      <RecipeFormLayout status={status}>
        {/* 409 Conflict Alert Banner */}
        {is409Conflict && (
          <div className="conflict-alert" role="alert">
            <div className="conflict-alert-content">
              <strong>⚠️ Xung đột phiên bản (409 Conflict)</strong>
              <p>
                Dữ liệu công thức đã được sửa đổi hoặc cập nhật bởi một phiên làm việc khác. Để đảm bảo tính toàn vẹn dữ liệu, vui lòng tải lại công thức.
              </p>
            </div>
            {onReload && (
              <button
                type="button"
                className="primary-button conflict-reload-btn"
                onClick={() => void handleReload()}
                disabled={isReloading}
              >
                {isReloading ? 'Đang tải lại...' : '🔄 Tải lại dữ liệu mới nhất'}
              </button>
            )}
          </div>
        )}

        {/* Success Feedback Banner */}
        {successMessage && (
          <div className="form-success-banner" role="status">
            <span>✅ {successMessage}</span>
          </div>
        )}

        {/* Section 01: Core Recipe Metadata */}
        <div className="form-section">
          <div className="form-section-heading">
            <span className="step-number">01</span>
            <div>
              <h2>Thông tin món ăn</h2>
              <p>Đặt nền tảng cho công thức: tên món, danh mục, thời gian, khẩu phần và độ khó.</p>
            </div>
          </div>

          <div className="form-grid">
            <label className="field field-wide" htmlFor="recipe-title">
              <span>Tên công thức * (5 - 200 ký tự)</span>
              <input
                id="recipe-title"
                required
                minLength={5}
                maxLength={200}
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                placeholder="Ví dụ: Phở bò tái lăn Hà Nội truyền thống"
                disabled={saving}
              />
            </label>

            <label className="field" htmlFor="recipe-category">
              <span>Danh mục món ăn *</span>
              <select
                id="recipe-category"
                required
                value={categoryId}
                onChange={(event) => setCategoryId(event.target.value)}
                disabled={saving}
              >
                <option value="" disabled>
                  -- Chọn danh mục --
                </option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="field" htmlFor="recipe-prep-time">
              <span>Thời gian chuẩn bị (phút) *</span>
              <input
                id="recipe-prep-time"
                required
                min="1"
                type="number"
                value={prepTimeMinutes}
                onChange={(event) => setPrepTimeMinutes(event.target.value)}
                disabled={saving}
              />
            </label>

            <label className="field" htmlFor="recipe-cook-time">
              <span>Thời gian nấu (phút) *</span>
              <input
                id="recipe-cook-time"
                required
                min="0"
                type="number"
                value={cookTimeMinutes}
                onChange={(event) => setCookTimeMinutes(event.target.value)}
                disabled={saving}
              />
            </label>

            <label className="field" htmlFor="recipe-servings">
              <span>Khẩu phần (người ăn) *</span>
              <input
                id="recipe-servings"
                required
                min="1"
                type="number"
                value={servings}
                onChange={(event) => setServings(event.target.value)}
                disabled={saving}
              />
            </label>

            <label className="field" htmlFor="recipe-difficulty">
              <span>Độ khó</span>
              <select
                id="recipe-difficulty"
                value={difficulty}
                onChange={(event) => {
                  const result = difficultyLevelSchema.safeParse(event.target.value);
                  if (result.success) setDifficulty(result.data);
                }}
                disabled={saving}
              >
                <option value="Easy">Easy (Dễ làm)</option>
                <option value="Medium">Medium (Trung bình)</option>
                <option value="Hard">Hard (Khó)</option>
                <option value="Expert">Expert (Chuyên gia)</option>
              </select>
            </label>

            <label className="field field-wide" htmlFor="recipe-description">
              <span>Mô tả ngắn gọn (Tối đa 2.000 ký tự)</span>
              <input
                id="recipe-description"
                maxLength={2000}
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                placeholder="Một đoạn giới thiệu ngắn cuốn hút về hương vị, xuất xứ hoặc nét đặc trưng của món ăn"
                disabled={saving}
              />
            </label>

            <label className="field field-wide" htmlFor="recipe-content">
              <span>Cách thực hiện tổng quát *</span>
              <textarea
                id="recipe-content"
                required
                rows={5}
                value={content}
                onChange={(event) => setContent(event.target.value)}
                placeholder="Mô tả tổng quan quy trình nấu, lưu ý khi chọn nguyên liệu hoặc bí quyết canh lửa..."
                disabled={saving}
              />
            </label>
          </div>
        </div>

        {/* Section 02: Ingredient Management */}
        <div className="form-section">
          <div className="form-section-heading">
            <span className="step-number">02</span>
            <div>
              <h2>Nguyên liệu</h2>
              <p>Thêm, sửa, xóa và sắp xếp nguyên liệu theo thứ tự chuẩn bị hoặc trình tự nấu.</p>
            </div>
          </div>
          <IngredientEditor
            ingredients={ingredients}
            onChange={setIngredients}
            disabled={saving}
          />
        </div>

        {/* Section 03: Recipe Steps (Võ Hùng Mạnh's StepEditor) */}
        <div className="form-section">
          <div className="form-section-heading">
            <span className="step-number">03</span>
            <div>
              <h2>Các bước thực hiện</h2>
              <p>Mô tả chi tiết từng bước nấu ăn. Cần ít nhất 1 bước để có thể xuất bản công thức.</p>
            </div>
          </div>
          <StepEditor steps={steps} onChange={setSteps} />
        </div>

        {/* Section 04: Nutrition Management (NutritionEditor) */}
        <div className="form-section">
          <div className="form-section-heading">
            <span className="step-number">04</span>
            <div>
              <h2>Giá trị dinh dưỡng</h2>
              <p>Hàm lượng dinh dưỡng tham khảo cho mỗi khẩu phần: Calories, Protein, Carbs, Fat, Fiber và Sodium.</p>
            </div>
          </div>
          <NutritionEditor
            nutrition={nutrition}
            onChange={setNutrition}
            disabled={saving}
          />
        </div>

        {/* Section 05: Photos & Media (Võ Hùng Mạnh's ImageManager) */}
        <div className="form-section">
          <div className="form-section-heading">
            <span className="step-number">05</span>
            <div>
              <h2>Hình ảnh món ăn</h2>
              <p>Tải lên nhiều hình ảnh minh họa sống động, dung lượng tối đa 5 MB mỗi ảnh.</p>
            </div>
          </div>
          <ImageManager
            images={images}
            onImagesChange={setImages}
            onFilesChange={setImageFiles}
          />
        </div>
      </RecipeFormLayout>

      {/* General Error Message */}
      {message && (
        <p className="form-error" role="alert">
          {message}
        </p>
      )}

      {/* Lifecycle Actions (Publish, Archive, Unpublish, Delete) */}
      {recipeId && (
        <RecipeActions
          status={status}
          disabled={saving}
          onAction={changeStatus}
          onDelete={deleteRecipe}
          publishBlocker={publishBlocker}
        />
      )}

      {/* Form Submission Actions */}
      <div className="form-actions">
        <a className="outline-button" href="/recipes">
          Hủy bỏ
        </a>
        <button
          className="primary-button"
          disabled={saving || !categoryId || (Boolean(recipeId) && !etag)}
          type="submit"
        >
          {saving
            ? 'Đang lưu...'
            : recipeId
              ? 'Lưu thay đổi (Save Changes)'
              : 'Lưu bản nháp (Save Draft)'}
        </button>
      </div>
    </form>
  );
}
