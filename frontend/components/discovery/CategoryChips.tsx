'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Category = { id: string; name: string; slug: string; recipeCount: number };
const icons = ['🍜', '🥗', '🍰', '☕', '🍝', '🥘', '🥑', '🍲'];

export default function CategoryChips() {
  const [categories, setCategories] = useState<Category[]>([]);
  useEffect(() => {
    apiFetch<Category[]>('/api/v1/categories', {}, false).then(setCategories).catch(() => setCategories([]));
  }, []);

  return (
    <div className="category-row" aria-label="Danh mục món ăn">
      <a className="category" href="/recipes"><span className="category-icon">✨</span>Tất cả</a>
      {categories.map((category, index) => (
        <a className="category" href={`/categories/${encodeURIComponent(category.slug)}`} key={category.id}>
          <span className="category-icon">{icons[index % icons.length]}</span>{category.name}
        </a>
      ))}
    </div>
  );
}
