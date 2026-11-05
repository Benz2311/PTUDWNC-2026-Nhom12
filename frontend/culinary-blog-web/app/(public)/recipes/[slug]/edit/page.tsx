'use client';

import React from 'react';
import { useParams } from 'next/navigation';
import ProtectedRoute from '@/components/auth/ProtectedRoute';
import RecipeEditor from '@/components/recipe-management/RecipeEditor';

export default function EditRecipePage() {
  const params = useParams();
  const slug = (params?.slug as string) || '';

  return (
    <ProtectedRoute roles={['Author', 'Admin']}>
      <main className="mx-auto max-w-5xl px-4 py-8 sm:px-6 lg:px-8">
        <div className="mb-8">
          <div className="flow-kicker text-xs font-bold text-emerald-800 uppercase tracking-wider mb-1">
            Chỉnh sửa công thức
          </div>
          <h1 className="flow-title text-3xl font-extrabold text-gray-900 tracking-tight">
            Cập nhật công thức ẩm thực
          </h1>
          <p className="text-gray-600 text-sm mt-1.5 leading-relaxed">
            Chỉnh sửa thông tin, nguyên liệu, hướng dẫn và hình ảnh của món ăn.
          </p>
        </div>
        {slug ? <RecipeEditor slug={slug} /> : null}
      </main>
    </ProtectedRoute>
  );
}

