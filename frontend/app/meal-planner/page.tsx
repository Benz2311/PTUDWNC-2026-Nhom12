'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import Image from 'next/image';
import SiteHeader from '@/components/SiteHeader';
import type { ApiError } from '@/lib/api';
import { getRecipes } from '@/lib/api/recipes';
import {
  checkShoppingListItem,
  deleteMealPlanEntry,
  deleteShoppingListItem,
  generateShoppingList,
  getMealPlan,
  upsertMealPlanEntry,
  type MealPlanEntry,
  type ShoppingListItem,
} from '@/lib/api/meal-plan';
import type { RecipeSummary } from '@/types/recipe';

const mealTypes = ['Breakfast', 'Lunch', 'Dinner', 'Snack'];
const mealLabels: Record<string, string> = {
  Breakfast: 'Bữa sáng',
  Lunch: 'Bữa trưa',
  Dinner: 'Bữa tối',
  Snack: 'Ăn nhẹ',
};

function formatDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function formatQuantity(quantity: number) {
  return new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(quantity);
}

function getIngredientCategory(name: string) {
  const normalized = name.toLocaleLowerCase('vi');
  if (['thịt', 'cá', 'tôm', 'mực', 'gà', 'bò', 'heo', 'trứng', 'đậu hũ'].some((term) => normalized.includes(term))) {
    return 'Thịt, cá & hải sản';
  }
  if (['rau', 'củ', 'quả', 'trái', 'hành', 'tỏi', 'ớt', 'nấm', 'cà chua', 'khoai', 'gừng', 'chanh'].some((term) => normalized.includes(term))) {
    return 'Rau củ & trái cây';
  }
  if (['muối', 'đường', 'nước mắm', 'dầu', 'tiêu', 'bột', 'gạo', 'mì', 'nước tương', 'xì dầu'].some((term) => normalized.includes(term))) {
    return 'Gia vị & đồ khô';
  }
  return 'Khác';
}

export default function MealPlannerPage() {
  const today = useMemo(() => new Date(), []);
  const [from, setFrom] = useState(formatDate(today));
  const [to, setTo] = useState(formatDate(new Date(today.getTime() + 6 * 86400000)));
  const [recipes, setRecipes] = useState<RecipeSummary[]>([]);
  const [mealPlan, setMealPlan] = useState<MealPlanEntry[]>([]);
  const [shopping, setShopping] = useState<ShoppingListItem[]>([]);
  const [openSlot, setOpenSlot] = useState<string | null>(null);
  const [message, setMessage] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);

  const dates = useMemo(() => {
    const start = new Date(`${from}T00:00:00`);
    const end = new Date(`${to}T00:00:00`);
    const result: string[] = [];
    for (let cursor = start; cursor <= end; cursor.setDate(cursor.getDate() + 1)) {
      result.push(formatDate(cursor));
    }
    return result.slice(0, 14);
  }, [from, to]);

  const load = useCallback(async () => {
    if (typeof window === 'undefined') return;

    const accessToken = localStorage.getItem('culinary_access_token');
    if (!accessToken) {
      setIsAuthenticated(false);
      setIsLoading(false);
      setMessage('Vui lòng đăng nhập để sử dụng Kế hoạch bữa ăn.');
      return;
    }

    setIsAuthenticated(true);
    setIsLoading(true);
    try {
      const [recipesPage, plan] = await Promise.all([
        getRecipes({ page: 1, pageSize: 100 }),
        getMealPlan(from, to),
      ]);
      const loadedRecipes = recipesPage && Array.isArray(recipesPage.items)
        ? recipesPage.items
        : [];
      const loadedPlan = Array.isArray(plan) ? plan : [];
      const list = await generateShoppingList(from, to);

      setRecipes(loadedRecipes);
      setMealPlan(loadedPlan);
      setShopping(Array.isArray(list) ? list : []);
      setMessage('');
    } catch (error) {
      if ((error as ApiError).status === 401) {
        setIsAuthenticated(false);
        setMessage('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
      } else {
        setMessage('Không thể tải kế hoạch và danh sách đi chợ.');
      }
    } finally {
      setIsLoading(false);
    }
  }, [from, to]);

  useEffect(() => { void load(); }, [load]);

  async function assign(date: string, mealType: string, recipeId: string) {
    try {
      await upsertMealPlanEntry(date, mealType, recipeId);
      setOpenSlot(null);
      await load();
      setMessage('Đã cập nhật kế hoạch và danh sách đi chợ.');
    } catch {
      setMessage('Không thể cập nhật kế hoạch.');
    }
  }

  async function removePlanEntry(id: string) {
    try {
      await deleteMealPlanEntry(id);
      await load();
      setMessage('Đã xóa món và cập nhật danh sách đi chợ.');
    } catch {
      setMessage('Không thể xóa món khỏi kế hoạch.');
    }
  }

  async function toggleItem(item: ShoppingListItem) {
    try {
      const updated = await checkShoppingListItem(item.id, !item.isChecked);
      setShopping((current) => current.map((entry) => entry.id === item.id ? updated : entry));
    } catch {
      setMessage('Không thể cập nhật trạng thái nguyên liệu.');
    }
  }

  async function removeItem(id: string) {
    try {
      await deleteShoppingListItem(id);
      setShopping((current) => current.filter((item) => item.id !== id));
    } catch {
      setMessage('Không thể xóa nguyên liệu khỏi danh sách.');
    }
  }

  const shoppingChecked = shopping.filter((item) => item.isChecked).length;
  const shoppingGroups = useMemo(() => {
    const groups = new Map<string, ShoppingListItem[]>();
    for (const item of shopping) {
      const category = getIngredientCategory(item.name);
      groups.set(category, [...(groups.get(category) ?? []), item]);
    }
    return [...groups.entries()];
  }, [shopping]);

  return (
    <div className="site-shell">
      <SiteHeader />
      <main className="content meal-planner-page">
        <header className="meal-planner-header">
          <div>
            <p className="eyebrow">Sắp xếp bữa ăn</p>
            <h1>Kế hoạch 7 ngày</h1>
            <p className="meal-planner-intro">
              💡 <strong>Cách dùng:</strong> Lên thực đơn cho các bữa ăn trong tuần, hệ thống sẽ tự động tổng hợp danh sách nguyên liệu đi chợ cho bạn.
            </p>
          </div>
          {isAuthenticated ? (
            <div className="meal-planner-dates">
              <label>
                <span>Từ ngày</span>
                <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
              </label>
              <label>
                <span>Đến ngày</span>
                <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
              </label>
            </div>
          ) : null}
        </header>

        {isLoading ? <p className="meal-planner-status" role="status">Đang tải kế hoạch...</p> : null}
        {!isLoading && isAuthenticated === false ? (
          <p className="meal-planner-status" role="status">
            {message} <a className="primary-button" href="/login">Đăng nhập</a>
          </p>
        ) : null}
        {isAuthenticated && message ? <p className="meal-planner-status" role="status">{message}</p> : null}

        {isAuthenticated ? (
          <div className="meal-planner-layout">
            <section className="meal-plan-panel" aria-labelledby="meal-plan-heading">
              <div className="meal-panel-heading">
                <div>
                  <p className="meal-panel-kicker">Thực đơn của bạn</p>
                  <h2 id="meal-plan-heading">Lên kế hoạch từng bữa</h2>
                </div>
                <span className="meal-plan-count">{mealPlan.length} bữa đã lên lịch</span>
              </div>
              <p className="meal-plan-change-note">
                Khi đổi món, nguyên liệu sẽ được tính lại. Mục đã tích chỉ giữ trạng thái nếu tên, đơn vị và lượng không đổi.
              </p>

              <div className="meal-plan-days">
                {dates.map((date) => {
                  const dateLabel = new Date(`${date}T00:00:00`).toLocaleDateString('vi-VN', {
                    weekday: 'short',
                    day: 'numeric',
                    month: 'numeric',
                  });

                  return (
                    <section className="meal-day" key={date} aria-label={dateLabel}>
                      <h3 className="meal-day-heading">{dateLabel}</h3>
                      <div className="meal-day-slots">
                        {mealTypes.map((type) => {
                          const entry = mealPlan.find((item) => item.date === date && item.mealType === type);
                          const slotKey = `${date}-${type}`;
                          const selectedRecipe = entry
                            ? recipes.find((recipe) => recipe.id === entry.recipeId)
                            : undefined;
                          const imageUrl = entry?.primaryImageUrl || selectedRecipe?.primaryImageUrl;

                          return (
                            <div className="meal-slot" key={type}>
                              <span className="meal-slot-label">{mealLabels[type]}</span>
                              {entry ? (
                                <div className="meal-card meal-card-selected">
                                  <a className="meal-card-link" href={`/recipes/${entry.recipeSlug}`}>
                                    {imageUrl ? (
                                      <Image
                                        className="meal-card-image"
                                        src={imageUrl}
                                        alt=""
                                        width={68}
                                        height={68}
                                        unoptimized
                                      />
                                    ) : (
                                      <span className="meal-card-image meal-card-placeholder" aria-hidden="true">🍲</span>
                                    )}
                                    <span className="meal-card-title">{entry.recipeTitle}</span>
                                  </a>
                                  <button
                                    className="meal-card-remove"
                                    type="button"
                                    aria-label={`Xóa ${entry.recipeTitle} khỏi ${mealLabels[type]}`}
                                    onClick={() => void removePlanEntry(entry.id)}
                                    disabled={isLoading}
                                  >
                                    ×
                                  </button>
                                  <button
                                    className="meal-card-change"
                                    type="button"
                                    onClick={() => setOpenSlot(openSlot === slotKey ? null : slotKey)}
                                    aria-expanded={openSlot === slotKey}
                                  >
                                    Đổi món
                                  </button>
                                </div>
                              ) : (
                                <button
                                  className="meal-card meal-card-empty"
                                  type="button"
                                  onClick={() => setOpenSlot(openSlot === slotKey ? null : slotKey)}
                                  aria-expanded={openSlot === slotKey}
                                >
                                  <span className="meal-card-add-icon" aria-hidden="true">+</span>
                                  <span>Thêm món</span>
                                </button>
                              )}
                              {openSlot === slotKey ? (
                                <label className="meal-recipe-picker">
                                  <span className="meal-recipe-picker-label">Chọn món ăn</span>
                                  {recipes.length > 0 ? (
                                    <select
                                      autoFocus
                                      value=""
                                      onChange={(event) => {
                                        if (event.target.value) void assign(date, type, event.target.value);
                                      }}
                                    >
                                      <option value="">Chọn từ công thức của bạn</option>
                                      {recipes.map((recipe) => (
                                        <option key={recipe.id} value={recipe.id}>{recipe.title}</option>
                                      ))}
                                    </select>
                                  ) : (
                                    <span className="meal-no-recipes">
                                      Chưa có công thức. <a href="/recipes/new">Tạo món mới</a>
                                    </span>
                                  )}
                                </label>
                              ) : null}
                            </div>
                          );
                        })}
                      </div>
                    </section>
                  );
                })}
              </div>
            </section>

            <aside className="shopping-panel" aria-labelledby="shopping-heading">
              <div className="shopping-panel-heading">
                <span className="shopping-cart-icon" aria-hidden="true">🛒</span>
                <div>
                  <p className="meal-panel-kicker">Tự động tổng hợp từ kế hoạch</p>
                  <h2 id="shopping-heading">Nguyên liệu cần mua</h2>
                </div>
              </div>
              <p className="shopping-panel-description">
                Danh sách này chứa tổng lượng nguyên liệu cần chuẩn bị cho các món bạn đã chọn ở trên.
              </p>
              <div className="shopping-progress">
                <span>Danh sách đi chợ tuần này</span>
                <strong>{shoppingChecked}/{shopping.length} đã có</strong>
              </div>
              {shopping.length === 0 ? (
                <div className="shopping-empty">
                  <span aria-hidden="true">🥬</span>
                  <strong>Danh sách đang trống</strong>
                  <p>Chọn món ở kế hoạch bên trái, nguyên liệu sẽ tự xuất hiện tại đây.</p>
                </div>
              ) : (
                <div className="shopping-groups">
                  {shoppingGroups.map(([category, items]) => (
                    <section className="shopping-group" key={category} aria-label={category}>
                      <h3 className="shopping-group-heading">{category}</h3>
                      <ul className="shopping-list">
                        {items.map((item) => (
                          <li className={`shopping-item${item.isChecked ? ' is-checked' : ''}`} key={item.id}>
                            <label className="shopping-item-label">
                              <input
                                type="checkbox"
                                checked={item.isChecked}
                                onChange={() => void toggleItem(item)}
                              />
                              <span className="shopping-item-details">
                                <strong>{item.name}</strong>
                                {item.quantity != null ? (
                                  <small>{formatQuantity(item.quantity)} {item.unit ?? ''}</small>
                                ) : null}
                              </span>
                            </label>
                            <button
                              type="button"
                              className="shopping-item-remove"
                              aria-label={`Xóa ${item.name}`}
                              onClick={() => void removeItem(item.id)}
                            >
                              ×
                            </button>
                          </li>
                        ))}
                      </ul>
                    </section>
                  ))}
                </div>
              )}
              {shopping.length > 0 ? (
                <button
                  type="button"
                  className="shopping-clear-button"
                  onClick={() => Promise.all(shopping.filter((item) => item.isChecked).map((item) => removeItem(item.id)))}
                  disabled={!shopping.some((item) => item.isChecked)}
                >
                  Xóa tất cả mục đã mua
                </button>
              ) : null}
            </aside>
          </div>
        ) : null}
      </main>
    </div>
  );
}
