'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Category = { id: string; name: string; slug: string; recipeCount: number };
const icons = ['🍜', '🥗', '🍰', '☕', '🍝', '🥘', '🥑', '🍲'];

export default function CategoryChips() {
  const [categories, setCategories] = useState<Category[]>([]);
  useEffect(() => {
    apiFetch<unknown>('/api/v1/categories', {}, false)
      .then((data) => {
        const rawList = Array.isArray(data)
          ? data
          : Array.isArray((data as Record<string, unknown>)?.value)
            ? (data as { value: unknown[] }).value
            : Array.isArray((data as Record<string, unknown>)?.items)
              ? (data as { items: unknown[] }).items
              : [];
        const normalized: Category[] = rawList.map((item) => {
          const cat = item as Record<string, unknown>;
          return {
            id: String(cat.id ?? cat.Id ?? ''),
            name: String(cat.name ?? cat.Name ?? cat.categoryName ?? ''),
            slug: String(cat.slug ?? cat.Slug ?? ''),
            recipeCount: Number(cat.recipeCount ?? cat.RecipeCount ?? 0),
          };
        });
        setCategories(normalized);
      })
      .catch(() => setCategories([]));
  }, []);

  return (
    <div className="category-row" aria-label="Danh mục món ăn">
      <a className="category" href="/recipes"><span className="category-icon">✨</span>Tất cả</a>
      {categories.map((category, index) => (
        <a className="category" href={`/categories/${encodeURIComponent(category.slug)}`} key={category.id || `chip-${index}`}>
          <span className="category-icon">{icons[index % icons.length]}</span>{category.name}
        </a>
      ))}
    </div>
  );
}
