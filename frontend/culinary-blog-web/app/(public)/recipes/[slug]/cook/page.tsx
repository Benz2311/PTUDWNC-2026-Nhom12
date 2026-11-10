import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { cache } from 'react';
import type { RecipeDetail } from '@/types/recipe';
import { normalizeRecipeStatus } from '@/types/recipe';
import { CookingMode } from '@/components/cooking/CookingMode';

type Props = { params: Promise<{ slug: string }> };

const getCookRecipe = cache(async (slug: string): Promise<RecipeDetail | null> => {
  const api = process.env.INTERNAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8088/api/v1';
  const base = api.replace(/\/+$/, '').replace(/\/api\/v1$/, '');

  try {
    const response = await fetch(`${base}/api/v1/recipes/${encodeURIComponent(slug)}`, {
      cache: 'no-store',
      signal: AbortSignal.timeout(4000),
    });
    if (!response.ok) return null;

    const body = (await response.json()) as { data?: RecipeDetail };
    const data = body.data;
    return data && normalizeRecipeStatus(data.status) === 'Published' ? data : null;
  } catch {
    return null;
  }
});

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const recipe = await getCookRecipe(slug);

  if (!recipe) {
    return {
      title: 'Chế độ nấu ăn | Culinary Blog',
    };
  }

  return {
    title: `Chế độ nấu ăn: ${recipe.title} | Culinary Blog`,
    description: `Hướng dẫn nấu ăn từng bước với đồng hồ hẹn giờ thông minh cho món ${recipe.title}.`,
  };
}

export default async function CookingModePage({ params }: Props) {
  const { slug } = await params;
  const recipe = await getCookRecipe(slug);

  if (!recipe) {
    notFound();
  }

  return <CookingMode recipe={recipe} />;
}
