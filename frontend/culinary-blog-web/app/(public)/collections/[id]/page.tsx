import React from "react";
import type { Metadata } from "next";
import { CollectionDetailPage } from "@/components/collections/CollectionDetailPage";

export const metadata: Metadata = {
  title: "Chi tiết bộ sưu tập | Culinary Blog",
  description: "Xem và quản lý các công thức trong bộ sưu tập.",
};

interface PageProps {
  params: Promise<{ id: string }>;
}

export default async function Page({ params }: PageProps) {
  const { id } = await params;
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <CollectionDetailPage id={id} />
    </div>
  );
}
