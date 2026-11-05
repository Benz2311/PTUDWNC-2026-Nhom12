'use client';

import React from 'react';
import ProtectedRoute from '@/components/auth/ProtectedRoute';
import RecipeForm from '@/components/recipe-management/RecipeForm';

export default function CreateRecipePage() {
  return (
    <ProtectedRoute roles={['Author', 'Admin']}>
      <main className="mx-auto max-w-5xl px-4 py-8 sm:px-6 lg:px-8">
        <div className="mb-8">
          <div className="flow-kicker text-xs font-bold text-emerald-800 uppercase tracking-wider mb-1">
            Tạo công thức mới
          </div>
          <h1 className="flow-title text-3xl font-extrabold text-gray-900 tracking-tight">
            Đăng tải công thức ẩm thực
          </h1>
          <p className="text-gray-600 text-sm mt-1.5 leading-relaxed">
            Chia sẻ các món ăn thơm ngon, bí quyết nấu nướng và câu chuyện ẩm thực cùng cộng đồng Culinary Blog.
          </p>
        </div>
        <RecipeForm />
      </main>
    </ProtectedRoute>
  );
}
