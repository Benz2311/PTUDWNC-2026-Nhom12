import type { Metadata } from "next";
import { cache } from "react";
import { normalizeRecipeStatus, type RecipeDetail } from "@/types/recipe";
import RecipeDetailClient from "./RecipeDetailClient";

type Props = { params: Promise<{ slug: string }> };

const getPublicRecipe = cache(async (slug: string): Promise<RecipeDetail | null> => {
  const api = process.env.INTERNAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL;
  if (!api) return null;

  const base = api.replace(/\/+$/, "").replace(/\/api\/v1$/, "");
  try {
    const response = await fetch(`${base}/api/v1/recipes/${encodeURIComponent(slug)}`, {
      cache: "no-store",
      signal: AbortSignal.timeout(3000),
    });
    if (!response.ok) return null;

    const { data } = await response.json() as { data?: RecipeDetail };
    return data && normalizeRecipeStatus(data.status) === "Published" ? data : null;
  } catch {
    return null;
  }
});

function publicOrigin(): URL | undefined {
  const value = process.env.PUBLIC_ORIGIN ?? process.env.NEXT_PUBLIC_API_URL;
  if (!value) return undefined;
  try {
    const url = new URL(value);
    return new URL(url.origin);
  } catch {
    return undefined;
  }
}

function recipeImage(recipe: RecipeDetail, origin: URL): string | undefined {
  const primary = recipe.images.find((image) => image.isPrimary) ?? recipe.images[0];
  const image = primary?.mediumUrl ?? primary?.originalUrl ?? recipe.coverImageUrl;
  if (!image) return undefined;
  try {
    const url = new URL(image, origin);
    if (url.protocol !== "http:" && url.protocol !== "https:") return undefined;
    if (url.pathname.startsWith("/culinaryblog/") &&
        ["localhost", "127.0.0.1", "minio"].includes(url.hostname)) {
      return new URL(url.pathname + url.search, origin).href;
    }
    return url.href;
  } catch {
    return undefined;
  }
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const recipe = await getPublicRecipe(slug);
  if (!recipe) return { robots: { index: false, follow: false } };

  const title = recipe.title;
  const description = recipe.description;
  const origin = publicOrigin();
  if (!origin) return { title, description };

  const url = new URL(`/recipes/${encodeURIComponent(recipe.slug)}`, origin).href;
  const image = recipeImage(recipe, origin);
  return {
    title,
    description,
    alternates: { canonical: url },
    openGraph: { type: "article", title, description, url, images: image ? [image] : [] },
    twitter: { card: image ? "summary_large_image" : "summary", title, description, images: image ? [image] : [] },
  };
}

export default async function RecipeDetailPage({ params }: Props) {
  const { slug } = await params;
  const recipe = await getPublicRecipe(slug);
  const origin = publicOrigin();
  const prep = recipe?.prepTimeMinutes ?? recipe?.prepTime;
  const cook = recipe?.cookTimeMinutes ?? recipe?.cookTime;
  const jsonLd = recipe && origin ? {
    "@context": "https://schema.org",
    "@type": "Recipe",
    name: recipe.title,
    description: recipe.description,
    image: recipeImage(recipe, origin),
    author: { "@type": "Person", name: recipe.author.displayName },
    datePublished: recipe.publishedAt ?? undefined,
    prepTime: prep === undefined ? undefined : `PT${prep}M`,
    cookTime: cook === undefined ? undefined : `PT${cook}M`,
    totalTime: prep === undefined || cook === undefined ? undefined : `PT${prep + cook}M`,
    recipeYield: String(recipe.servings),
    recipeCategory: recipe.category.name,
    recipeIngredient: recipe.ingredients.map((item) =>
      [item.quantity, item.unit, item.name].filter((part) => part != null && part !== "").join(" ")),
    recipeInstructions: recipe.steps.map((step) => ({
      "@type": "HowToStep",
      name: step.title ?? undefined,
      text: step.description,
    })),
  } : undefined;

  return (
    <>
      {jsonLd && <script type="application/ld+json" dangerouslySetInnerHTML={{
        __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c"),
      }} />}
      <RecipeDetailClient />
    </>
  );
}
