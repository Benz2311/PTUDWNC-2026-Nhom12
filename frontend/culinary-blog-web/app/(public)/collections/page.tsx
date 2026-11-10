import React from "react";
import type { Metadata } from "next";
import { CollectionsPage } from "@/components/collections/CollectionsPage";

export const metadata: Metadata = {
  title: "Bộ sưu tập công thức | Culinary Blog",
  description: "Quản lý và sắp xếp các bộ sưu tập công thức món ăn yêu thích của bạn.",
};

export default function Page() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <CollectionsPage />
    </div>
  );
}
