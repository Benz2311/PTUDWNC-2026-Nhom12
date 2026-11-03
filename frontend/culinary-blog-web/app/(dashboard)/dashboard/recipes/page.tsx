"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { BookOpen, PlusCircle } from "lucide-react";
import ProtectedRoute from "@/components/auth/ProtectedRoute";
import { getRecipes } from "@/lib/api/recipes";
import type { RecipeListItem } from "@/types/recipe";

function RecipeManagementPage() {
  const [recipes, setRecipes] = useState<RecipeListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;
    getRecipes({ mine: true, page: 1, pageSize: 50 }).then((result) => {
      if (active) {
        setRecipes(result.items);
        setIsLoading(false);
      }
    });
    return () => {
      active = false;
    };
  }, []);

  return (
    <main className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Quản lý công thức</h1>
          <p className="mt-2 text-gray-600">
            Xem và tiếp tục quản lý các công thức bạn đã tạo.
          </p>
        </div>
        <Link
          href="/recipes/create"
          className="inline-flex items-center gap-2 rounded-lg bg-emerald-700 px-4 py-2.5 text-sm font-semibold text-white hover:bg-emerald-800"
        >
          <PlusCircle className="h-4 w-4" />
          Tạo công thức mới
        </Link>
      </div>

      {isLoading ? (
        <p className="mt-8 rounded-xl bg-white p-8 text-center text-gray-500">
          Đang tải công thức...
        </p>
      ) : recipes.length === 0 ? (
        <div className="mt-8 rounded-xl border border-dashed border-gray-300 bg-white p-10 text-center">
          <BookOpen className="mx-auto h-10 w-10 text-gray-400" />
          <p className="mt-3 text-gray-600">Bạn chưa có công thức nào.</p>
        </div>
      ) : (
        <div className="mt-8 overflow-hidden rounded-xl border border-gray-200 bg-white">
          <div className="divide-y divide-gray-100">
            {recipes.map((recipe) => (
              <div key={recipe.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
                <div>
                  <Link href={`/recipes/${recipe.slug}`} className="font-semibold text-gray-900 hover:text-emerald-700">
                    {recipe.title}
                  </Link>
                  <p className="mt-1 text-sm text-gray-500">
                    Trạng thái: {String(recipe.status ?? "Draft")}
                  </p>
                </div>
                <Link
                  href={`/recipes/${recipe.slug}`}
                  className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
                >
                  Xem công thức
                </Link>
              </div>
            ))}
          </div>
        </div>
      )}
    </main>
  );
}

export default function RecipeManagementRoute() {
  return (
    <ProtectedRoute roles={["Author", "Admin"]}>
      <RecipeManagementPage />
    </ProtectedRoute>
  );
}
