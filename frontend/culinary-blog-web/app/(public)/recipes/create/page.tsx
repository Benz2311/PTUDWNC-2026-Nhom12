'use client';

import React from 'react';
import Link from 'next/link';
import { ChefHat, ArrowLeft, Sparkles, BookOpen } from 'lucide-react';
import { Button } from '@/components/ui/Button';

export default function CreateRecipePage() {
  return (
    <div className="min-h-[75vh] flex items-center justify-center px-4 py-16 bg-gradient-to-b from-emerald-50/50 via-white to-slate-50">
      <div className="max-w-xl w-full text-center p-8 sm:p-10 bg-white rounded-3xl border border-emerald-100 shadow-xl space-y-6">
        <div className="mx-auto w-16 h-16 rounded-2xl bg-gradient-to-br from-amber-100 to-amber-200 text-amber-800 flex items-center justify-center shadow-inner">
          <ChefHat className="w-8 h-8" />
        </div>

        <div className="space-y-3">
          <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 shadow-2xs">
            <Sparkles className="w-3.5 h-3.5 text-amber-500" />
            <span>Kế hoạch phát triển Task 8</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 tracking-tight">
            Chức năng Tạo công thức đang được phát triển
          </h1>
          <p className="text-sm text-gray-600 leading-relaxed max-w-md mx-auto">
            Tính năng đăng tải và quản lý công thức mới của người dùng thuộc giai đoạn tiếp theo (Task 8). Bạn có thể quay lại trang chủ để khám phá hàng chục công thức thơm ngon hiện có.
          </p>
        </div>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
          <Link href="/" className="w-full sm:w-auto">
            <Button variant="default" className="w-full sm:w-auto inline-flex items-center justify-center gap-2 rounded-xl px-5 py-2.5 font-bold shadow-sm">
              <ArrowLeft className="w-4 h-4" />
              <span>Quay về trang chủ</span>
            </Button>
          </Link>
          <Link href="/categories" className="w-full sm:w-auto">
            <Button variant="outline" className="w-full sm:w-auto inline-flex items-center justify-center gap-2 rounded-xl px-5 py-2.5 font-bold border-gray-200">
              <BookOpen className="w-4 h-4 text-emerald-700" />
              <span>Khám phá danh mục</span>
            </Button>
          </Link>
        </div>
      </div>
    </div>
  );
}
