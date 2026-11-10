'use client';

import { FormEvent, useEffect, useState, useMemo, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { apiJson, type ApiError } from '@/lib/api';
import {
  changeRecipeStatus,
  createRecipe,
  deleteRecipe as deleteRecipeRequest,
  updateRecipe,
} from '@/lib/api/recipes';
import Link from 'next/link';
import { ImageManager } from './ImageManager';
import type { RecipeImage } from '@/types/image';
import IngredientEditor, { IngredientDraft } from './IngredientEditor';
import NutritionEditor, { NutritionData } from './NutritionEditor';
import RecipeActions, { RecipeLifecycleAction } from './RecipeActions';
import { StepEditor, type StepDraft } from './StepEditor';
import { uploadStepImageFile } from '@/lib/api/steps';
import { uploadRecipeImageFile } from '@/lib/api/images';
import { parseFractionOrDecimal } from '@/lib/recipe-utils';
import {
  difficultyLevelSchema,
  recipeDraftRequestSchema,
  normalizeRecipeStatus,
  normalizeRecipeDifficulty,
  type DifficultyLevel,
  type RecipeDetail,
  type RecipeStatus,
} from '@/types/recipe';
import RecipeFormLayout from '@/components/recipes/RecipeFormLayout';

type Category = { id: string; name: string };
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
  const [steps, setSteps] = useState<StepDraft[]>([
    { title: '', description: '', timerMinutes: '', imageUrl: '' },
  ]);
  const [images, setImages] = useState<RecipeImage[]>([]);
  const [nutrition, setNutrition] = useState<NutritionData>({
    calories: '0',
    protein: '0',
    carbohydrates: '0',
    fat: '0',
    fiber: '0',
    sodium: '0',
  });

  // State management & lifecycle
  const [status, setStatus] = useState<RecipeStatus>(normalizeRecipeStatus(initialRecipe?.status));
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

    setTitle(initialRecipe.title ?? '');
    setStatus(normalizeRecipeStatus(initialRecipe.status));
    setDescription(initialRecipe.description ?? '');
    setContent(initialRecipe.content ?? '');
    setCategoryId(initialRecipe.categoryId ?? '');
    setPrepTimeMinutes(String(initialRecipe.prepTimeMinutes));
    setCookTimeMinutes(String(initialRecipe.cookTimeMinutes));
    setServings(String(initialRecipe.servings));
    setDifficulty(normalizeRecipeDifficulty(initialRecipe.difficulty));

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
        ? initialRecipe.steps.map((s) => ({
            id: s.id,
            stepNumber: s.stepNumber,
            title: s.title ?? '',
            description: s.description ?? '',
            timerMinutes: s.timerMinutes !== null && s.timerMinutes !== undefined ? String(s.timerMinutes) : '',
            imageUrl: s.imageUrl ?? '',
          }))
        : [{ title: '', description: '', timerMinutes: '', imageUrl: '' }],
    );

    setImages(
      initialRecipe.images.map((image) => ({
        id: image.id,
        recipeId: (image as any).recipeId || initialRecipe.id,
        originalUrl: (image as any).originalUrl || (image as any).url || '',
        mediumUrl: (image as any).mediumUrl || (image as any).url || '',
        thumbnailUrl: (image as any).thumbnailUrl || (image as any).url || '',
        altText: image.altText ?? initialRecipe.title,
        isPrimary: image.isPrimary ?? false,
        orderIndex: image.orderIndex ?? (image as any).sortOrder ?? 0,
      }))
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
    if (!title.trim() || title.trim().length < 3 || title.trim().length > 200) {
      return 'Tiêu đề công thức phải từ 3 đến 200 ký tự.';
    }
    if (description.trim().length < 10 || description.trim().length > 2000) {
      return 'Mô tả phải từ 10 đến 2.000 ký tự.';
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
    description,
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

    try {
      if (recipeId && !etag) {
        throw new Error('Không nhận được phiên bản công thức (ETag). Hãy tải lại trang trước khi lưu.');
      }

      // Trong chế độ chỉnh sửa (Edit mode): nếu có bước nào vừa chọn file chưa tải lên, upload lên MinIO trước
      const currentSteps = [...steps];
      if (recipeId) {
        await Promise.all(
          currentSteps.map(async (s, i) => {
            if (s.imageFile && (!s.imageUrl || s.uploadStatus === 'error')) {
              try {
                const res = await uploadStepImageFile(recipeId, s.imageFile);
                if (res?.url) {
                  currentSteps[i] = {
                    ...s,
                    imageUrl: res.url,
                    imageFile: null,
                    imagePreviewUrl: null,
                    uploadStatus: 'success',
                    uploadError: null,
                  };
                }
              } catch (err) {
                throw new Error(`Tải ảnh bước ${i + 1} thất bại: ${err instanceof Error ? err.message : 'Lỗi kết nối'}`);
              }
            }
          })
        );
      }

      const requestPayload = {
        categoryId,
        title: title.trim(),
        description: description.trim(),
        content: content.trim(),
        prepTimeMinutes: Number(prepTimeMinutes),
        cookTimeMinutes: Number(cookTimeMinutes),
        servings: Number(servings),
        difficulty: normalizeRecipeDifficulty(difficulty),
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
            quantity: parseFractionOrDecimal(ingredient.quantity),
            unit: ingredient.unit.trim() || null,
            notes: ingredient.notes.trim() || null,
            orderIndex: ingredient.orderIndex !== undefined ? ingredient.orderIndex : index,
          })),
        images: (images as any[])
          .filter((img) => {
            const u = img.url || img.originalUrl;
            return Boolean(u && typeof u === 'string' && u.trim().length > 0 && !u.startsWith('blob:'));
          })
          .map((img) => ({
            url: (img.url || img.originalUrl).trim(),
            altText: img.altText || null,
          })),
        steps: currentSteps
          .filter((step) => step.description.trim())
          .map((step) => {
            const timer =
              step.timerMinutes !== '' && step.timerMinutes !== null && step.timerMinutes !== undefined
                ? Math.round(Number(step.timerMinutes))
                : null;
            // Tuyệt đối không ghi URL dạng 'blob:' vào PostgreSQL!
            const validImageUrl =
              step.imageUrl &&
              typeof step.imageUrl === 'string' &&
              step.imageUrl.trim().length > 0 &&
              !step.imageUrl.startsWith('blob:')
                ? step.imageUrl.trim()
                : null;
            return {
              title: step.title.trim(),
              description: step.description.trim(),
              timerMinutes: timer !== null && !isNaN(timer) && timer >= 0 ? timer : null,
              imageUrl: validImageUrl,
            };
          }),
      };

      const validation = recipeDraftRequestSchema.safeParse(requestPayload);
      if (!validation.success) {
        const firstError = validation.error.issues[0]?.message;
        throw new Error(
          firstError || 'Dữ liệu công thức chưa hợp lệ. Vui lòng kiểm tra lại các trường thông tin.',
        );
      }

      if (recipeId) {
        const response = await updateRecipe(recipeId, validation.data, etag);
        const newEtag = ('version' in response && response.version) ? String(response.version) : etag;
        setEtag(newEtag);
        setSuccessMessage('Đã lưu thay đổi công thức thành công!');
        router.push(`/recipes/${response.slug}`);
      } else {
        // CHẾ ĐỘ TẠO MỚI (Create Mode)
        // 1. Tạo bản nháp công thức trong PostgreSQL để lấy Recipe ID và ETag
        const response = await createRecipe(validation.data);
        const newRecipeId = response.id;
        const newSlug = response.slug;
        const newEtag = ('version' in response && response.version) ? String(response.version) : etag;

        // 2. Tải lên ảnh món ăn nếu người dùng đã chọn file từ máy trong ImageManager
        const imagesWithFiles = (images as any[]).filter((img) => Boolean(img.file));
        if (imagesWithFiles.length > 0) {
          await Promise.all(
            imagesWithFiles.map(async (img) => {
              try {
                await uploadRecipeImageFile(
                  newRecipeId,
                  img.file,
                  img.altText || undefined,
                  img.isPrimary
                );
              } catch (upErr) {
                console.error('Tải ảnh món ăn thất bại:', upErr);
              }
            })
          );
        }

        // 3. Tải lên ảnh bước nấu cho những bước người dùng đã chọn file từ máy
        const stepsWithFiles = currentSteps.filter((s) => Boolean(s.imageFile));
        if (stepsWithFiles.length > 0) {
          const uploadErrors: string[] = [];
          await Promise.all(
            currentSteps.map(async (s, i) => {
              if (s.imageFile) {
                try {
                  const res = await uploadStepImageFile(newRecipeId, s.imageFile);
                  if (res?.url) {
                    currentSteps[i] = {
                      ...s,
                      imageUrl: res.url,
                      imageFile: null,
                      imagePreviewUrl: null,
                      uploadStatus: 'success',
                      uploadError: null,
                    };
                  }
                } catch (upErr) {
                  const errMsg = upErr instanceof Error ? upErr.message : 'Tải ảnh thất bại';
                  uploadErrors.push(`Bước ${i + 1} (${errMsg})`);
                  currentSteps[i] = {
                    ...s,
                    uploadStatus: 'error',
                    uploadError: errMsg,
                  };
                }
              }
            })
          );

          // 3. Nếu có ít nhất 1 ảnh tải lên thành công, cập nhật lại steps trong PostgreSQL
          const hasUploadedStepImages = currentSteps.some(
            (s) => s.imageUrl && !s.imageUrl.startsWith('blob:')
          );
          if (hasUploadedStepImages) {
            const updatedStepPayloads = currentSteps
              .filter((step) => step.description.trim())
              .map((step) => {
                const timer =
                  step.timerMinutes !== '' && step.timerMinutes !== null && step.timerMinutes !== undefined
                    ? Math.round(Number(step.timerMinutes))
                    : null;
                const cleanUrl =
                  step.imageUrl &&
                  typeof step.imageUrl === 'string' &&
                  step.imageUrl.trim().length > 0 &&
                  !step.imageUrl.startsWith('blob:')
                    ? step.imageUrl.trim()
                    : null;
                return {
                  title: step.title.trim(),
                  description: step.description.trim(),
                  timerMinutes: timer !== null && !isNaN(timer) && timer >= 0 ? timer : null,
                  imageUrl: cleanUrl,
                };
              });

            await updateRecipe(
              newRecipeId,
              {
                ...validation.data,
                steps: updatedStepPayloads,
              },
              newEtag
            );
          }

          // 4. Nếu có bước nào bị lỗi tải ảnh, báo rõ cho người dùng và không chuyển trang
          if (uploadErrors.length > 0) {
            setSteps(currentSteps);
            setMessage(
              `Công thức đã tạo nháp thành công, nhưng ảnh bước nấu tải lên thất bại: ${uploadErrors.join('; ')}. Vui lòng thử lại.`
            );
            setSaving(false);
            return;
          }
        }

        setSuccessMessage('Đã tạo bản nháp công thức thành công!');
        router.push(`/recipes/${newSlug}`);
      }
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
  const changeStatus = useCallback(async (action: RecipeLifecycleAction) => {
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
      const result = await changeRecipeStatus(recipeId, action, etag);
      const newEtag = ('version' in result && (result as any).version) ? String((result as any).version) : etag;
      const normalizedStatus = normalizeRecipeStatus(result.status);
      setStatus(normalizedStatus);
      setEtag(newEtag);

      const statusMap: Record<RecipeStatus, string> = {
        Draft: 'Bản nháp',
        Published: 'Đã xuất bản',
        Archived: 'Đã lưu trữ',
      };
      setSuccessMessage(`Đã cập nhật trạng thái thành công: ${statusMap[normalizedStatus] || normalizedStatus}.`);
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
  }, [recipeId, etag, publishBlocker]);

  // Delete recipe
  const deleteRecipe = useCallback(async () => {
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
  }, [recipeId, etag, router]);

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
                onClick={() => {
                  handleReload().catch(() => {});
                }}
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
        <div className="form-section bg-white rounded-2xl border border-gray-200/80 shadow-xs p-6 sm:p-8 space-y-6">
          <div className="form-section-heading flex items-start gap-3.5 pb-4 border-b border-gray-100">
            <span className="step-number w-8 h-8 rounded-xl bg-emerald-100 text-emerald-800 font-extrabold flex items-center justify-center text-xs shrink-0 shadow-2xs">01</span>
            <div>
              <h2 className="text-lg sm:text-xl font-bold text-gray-900 tracking-tight">Thông tin món ăn</h2>
              <p className="text-xs sm:text-sm text-gray-500 mt-0.5">Đặt nền tảng cho công thức: tên món, danh mục, thời gian, khẩu phần và độ khó.</p>
            </div>
          </div>

          <div className="form-grid grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
            <label className="field field-wide col-span-1 sm:col-span-2 lg:col-span-4 block" htmlFor="recipe-title">
              <span className="block text-xs sm:text-sm font-semibold text-gray-700 mb-1.5">Tên công thức * (3 - 200 ký tự)</span>
              <input
                id="recipe-title"
                required
                minLength={3}
                maxLength={200}
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                placeholder="Ví dụ: Phở bò tái lăn Hà Nội truyền thống"
                disabled={saving}
                className="w-full px-4 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all text-sm shadow-2xs"
              />
            </label>

            <label className="field col-span-1 sm:col-span-2 block" htmlFor="recipe-category">
              <span className="block text-xs sm:text-sm font-semibold text-gray-700 mb-1.5">Danh mục món ăn *</span>
              <select
                id="recipe-category"
                required
                value={categoryId}
                onChange={(event) => setCategoryId(event.target.value)}
                disabled={saving}
                className="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all text-sm shadow-2xs cursor-pointer"
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

            <label className="field col-span-1 block" htmlFor="recipe-difficulty">
              <span className="block text-xs sm:text-sm font-semibold text-gray-700 mb-1.5">Độ khó</span>
              <select
                id="recipe-difficulty"
                value={difficulty}
                onChange={(event) => {
                  const result = difficultyLevelSchema.safeParse(event.target.value);
                  if (result.success) setDifficulty(result.data);
                }}
                disabled={saving}
                className="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all text-sm shadow-2xs cursor-pointer"
              >
                <option value="Easy">Easy (Dễ làm)</option>
                <option value="Medium">Medium (Trung bình)</option>
                <option value="Hard">Hard (Khó)</option>
                <option value="Expert">Expert (Chuyên gia)</option>
              </select>
            </label>

            <label className="field col-span-1 block" htmlFor="recipe-servings">
              <span className="block text-xs sm:text-sm font-semibold text-gray-700 mb-1.5">Khẩu phần (người ăn) *</span>
              <input
                id="recipe-servings"
                required
                min="1"
                type="number"
                value={servings}
                onChange={(event) => setServings(event.target.value)}
                disabled={saving}
                className="w-full px-4 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all text-sm shadow-2xs"
              />
            </label>

            <label className="field col-span-1 block" htmlFor="recipe-prep-time">
              <span className="block text-xs sm:text-sm font-semibold text-gray-700 mb-1.5">Thời gian chuẩn bị (phút) *</span>
              <input
                id="recipe-prep-time"
                required
                min="1"
                type="number"
                value={prepTimeMinutes}
                onChange={(event) => setPrepTimeMinutes(event.target.value)}
                disabled={saving}
                className="w-full px-4 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all text-sm shadow-2xs"
              />
            </label>

            <label className="field col-span-1 block" htmlFor="recipe-cook-time">
              <span className="block text-xs sm:text-sm font-semibold text-gray-700 mb-1.5">Thời gian nấu (phút) *</span>
              <input
                id="recipe-cook-time"
                required
                min="0"
                type="number"
                value={cookTimeMinutes}
                onChange={(event) => setCookTimeMinutes(event.target.value)}
                disabled={saving}
                className="w-full px-4 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all text-sm shadow-2xs"
              />
            </label>

            <label className="field field-wide col-span-1 sm:col-span-2 lg:col-span-4 block" htmlFor="recipe-description">
              <span className="block text-xs sm:text-sm font-semibold text-gray-700 mb-1.5">Mô tả ngắn gọn * (10 - 2.000 ký tự)</span>
              <input
                id="recipe-description"
                required
                minLength={10}
                maxLength={2000}
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                placeholder="Một đoạn giới thiệu ngắn cuốn hút về hương vị, xuất xứ hoặc nét đặc trưng của món ăn"
                disabled={saving}
                className="w-full px-4 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all text-sm shadow-2xs"
              />
            </label>

            <label className="field field-wide col-span-1 sm:col-span-2 lg:col-span-4 block" htmlFor="recipe-content">
              <span className="block text-xs sm:text-sm font-semibold text-gray-700 mb-1.5">Ghi chú thêm (không bắt buộc)</span>
              <textarea
                id="recipe-content"
                rows={4}
                value={content}
                onChange={(event) => setContent(event.target.value)}
                placeholder="Mô tả tổng quan quy trình nấu, lưu ý khi chọn nguyên liệu hoặc bí quyết canh lửa..."
                disabled={saving}
                className="w-full px-4 py-2.5 rounded-xl border border-gray-300 bg-white text-gray-900 placeholder:text-gray-400 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all text-sm shadow-2xs leading-relaxed"
              />
            </label>
          </div>
        </div>

        {/* Section 02: Ingredient Management */}
        <div className="form-section bg-white rounded-2xl border border-gray-200/80 shadow-xs p-6 sm:p-8 space-y-6">
          <div className="form-section-heading flex items-start gap-3.5 pb-4 border-b border-gray-100">
            <span className="step-number w-8 h-8 rounded-xl bg-emerald-100 text-emerald-800 font-extrabold flex items-center justify-center text-xs shrink-0 shadow-2xs">02</span>
            <div>
              <h2 className="text-lg sm:text-xl font-bold text-gray-900 tracking-tight">Nguyên liệu</h2>
              <p className="text-xs sm:text-sm text-gray-500 mt-0.5">Thêm, sửa, xóa và sắp xếp nguyên liệu theo thứ tự chuẩn bị hoặc trình tự nấu.</p>
            </div>
          </div>
          <IngredientEditor
            ingredients={ingredients}
            onChange={setIngredients}
            disabled={saving}
          />
        </div>

        {/* Section 03: Recipe Steps (Võ Hùng Mạnh's StepEditor) */}
        <div className="form-section bg-white rounded-2xl border border-gray-200/80 shadow-xs p-6 sm:p-8 space-y-6">
          <div className="form-section-heading flex items-start gap-3.5 pb-4 border-b border-gray-100">
            <span className="step-number w-8 h-8 rounded-xl bg-emerald-100 text-emerald-800 font-extrabold flex items-center justify-center text-xs shrink-0 shadow-2xs">03</span>
            <div>
              <h2 className="text-lg sm:text-xl font-bold text-gray-900 tracking-tight">Các bước thực hiện</h2>
              <p className="text-xs sm:text-sm text-gray-500 mt-0.5">Mô tả chi tiết từng bước nấu ăn. Cần ít nhất 1 bước để có thể xuất bản công thức.</p>
            </div>
          </div>
          <StepEditor steps={steps} onChange={setSteps} recipeId={recipeId} />
        </div>

        {/* Section 04: Nutrition Management (NutritionEditor) */}
        <div className="form-section bg-white rounded-2xl border border-gray-200/80 shadow-xs p-6 sm:p-8 space-y-6">
          <div className="form-section-heading flex items-start gap-3.5 pb-4 border-b border-gray-100">
            <span className="step-number w-8 h-8 rounded-xl bg-emerald-100 text-emerald-800 font-extrabold flex items-center justify-center text-xs shrink-0 shadow-2xs">04</span>
            <div>
              <h2 className="text-lg sm:text-xl font-bold text-gray-900 tracking-tight">Giá trị dinh dưỡng</h2>
              <p className="text-xs sm:text-sm text-gray-500 mt-0.5">Hàm lượng dinh dưỡng tham khảo cho mỗi khẩu phần: Calories, Protein, Carbs, Fat, Fiber và Sodium.</p>
            </div>
          </div>
          <NutritionEditor
            nutrition={nutrition}
            onChange={setNutrition}
            disabled={saving}
          />
        </div>

        {/* Section 05: Photos & Media (Võ Hùng Mạnh's ImageManager) */}
        <div className="form-section bg-white rounded-2xl border border-gray-200/80 shadow-xs p-6 sm:p-8 space-y-6">
          <div className="form-section-heading flex items-start gap-3.5 pb-4 border-b border-gray-100">
            <span className="step-number w-8 h-8 rounded-xl bg-emerald-100 text-emerald-800 font-extrabold flex items-center justify-center text-xs shrink-0 shadow-2xs">05</span>
            <div>
              <h2 className="text-lg sm:text-xl font-bold text-gray-900 tracking-tight">Hình ảnh món ăn</h2>
              <p className="text-xs sm:text-sm text-gray-500 mt-0.5">Tải lên nhiều hình ảnh minh họa sống động, dung lượng tối đa 5 MB mỗi ảnh.</p>
            </div>
          </div>
          <ImageManager
            recipeId={recipeId}
            authorId={initialRecipe?.authorId}
            initialImages={initialRecipe?.images as any}
            images={images as any}
            onImagesChange={(imgs: any) => setImages(imgs)}
            disabled={saving}
          />
        </div>
      </RecipeFormLayout>

      {/* General Error Message */}
      {message && (
        <div className="form-error p-4 bg-rose-50 border border-rose-200 rounded-xl text-sm font-medium text-rose-700 my-6 flex items-center gap-2 shadow-2xs" role="alert">
          <span>⚠️ {message}</span>
        </div>
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
      <div className="form-actions flex flex-col-reverse sm:flex-row items-center justify-end gap-3 pt-6 border-t border-gray-200 mt-8">
        <Link
          className="outline-button w-full sm:w-auto px-5 py-2.5 rounded-xl text-sm font-semibold text-gray-700 bg-white hover:bg-gray-100 border border-gray-300 transition-all text-center cursor-pointer shadow-2xs"
          href="/recipes"
        >
          Hủy bỏ
        </Link>
        <button
          className="primary-button w-full sm:w-auto px-6 py-2.5 rounded-xl text-sm font-bold text-white bg-emerald-700 hover:bg-emerald-800 disabled:opacity-50 shadow-sm transition-all flex items-center justify-center gap-2 cursor-pointer"
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
