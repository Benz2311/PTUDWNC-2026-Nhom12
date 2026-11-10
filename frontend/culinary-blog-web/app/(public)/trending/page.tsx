import React from "react";
import type { Metadata } from "next";
import { TrendingRecipesView } from "@/components/recipes/TrendingRecipesView";

export const metadata: Metadata = {
  title: "Công thức thịnh hành | Culinary Blog",
  description: "Khám phá các món ăn được xem và yêu thích nhiều nhất trong 7 ngày và 30 ngày qua.",
};

export default function TrendingPage() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <TrendingRecipesView />
    </div>
  );
}
