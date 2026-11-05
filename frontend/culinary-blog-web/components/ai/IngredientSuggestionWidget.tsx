"use client";

import Link from "next/link";
import { useState } from "react";
import { phaseFourApi, type SuggestedRecipe } from "@/lib/api/phase-four";
import PhaseFourNavigation from "@/components/phase-four/PhaseFourNavigation";

export default function IngredientSuggestionWidget() {
  const [ingredientsText, setIngredientsText] = useState("");
  const [recipes, setRecipes] = useState<SuggestedRecipe[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [searched, setSearched] = useState(false);

  const suggest = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const ingredients = ingredientsText.split(/[,\n;]/).map((value) => value.trim()).filter(Boolean);
    if (ingredients.length === 0) {
      setError("Nhập ít nhất một nguyên liệu bạn đang có.");
      return;
    }

    setLoading(true);
    setError("");
    setSearched(true);
    try {
      setRecipes(await phaseFourApi.suggestRecipes(ingredients));
    } catch {
      setError("Không thể tạo gợi ý lúc này. Hãy thử lại sau hoặc kiểm tra cấu hình dịch vụ AI.");
      setRecipes([]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="mx-auto max-w-4xl px-4 py-10">
      <PhaseFourNavigation />
      <header className="mb-8">
        <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">Trợ lý nhà bếp</p>
        <h1 className="mt-1 text-3xl font-bold text-slate-950">Hôm nay ăn gì?</h1>
        <p className="mt-2 text-slate-600">Cho biết nguyên liệu sẵn có để tìm công thức phù hợp trong thư viện.</p>
      </header>

      <form onSubmit={suggest} className="space-y-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <label htmlFor="available-ingredients" className="block font-semibold text-slate-900">Nguyên liệu (phân cách bằng dấu phẩy hoặc xuống dòng)</label>
        <textarea
          id="available-ingredients"
          value={ingredientsText}
          onChange={(event) => setIngredientsText(event.target.value)}
          rows={4}
          maxLength={1500}
          placeholder={"Ví dụ: cà chua, hành tây, ức gà"}
          className="w-full rounded-xl border border-slate-300 px-4 py-3"
        />
        {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
        <button type="submit" disabled={loading} className="rounded-lg bg-emerald-700 px-5 py-2.5 font-semibold text-white disabled:opacity-60">
          {loading ? "Đang tìm công thức…" : "Gợi ý món ăn"}
        </button>
      </form>

      {searched && !loading && !error && (
        <section className="mt-8" aria-live="polite">
          <h2 className="mb-4 text-xl font-bold text-slate-900">Công thức phù hợp</h2>
          {recipes.length === 0 ? (
            <p className="rounded-xl bg-slate-50 p-4 text-slate-600">Chưa tìm thấy công thức phù hợp trong thư viện.</p>
          ) : (
            <ul className="grid gap-4 sm:grid-cols-2">
              {recipes.map((recipe) => (
                <li key={recipe.id} className="rounded-xl border border-slate-200 bg-white p-4">
                  <Link href={`/recipes/${encodeURIComponent(recipe.slug)}`} className="font-bold text-emerald-800 hover:underline">{recipe.title}</Link>
                  <p className="mt-2 text-sm text-slate-600">{recipe.ingredients.join(", ")}</p>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </main>
  );
}
