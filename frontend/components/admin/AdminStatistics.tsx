'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Statistics = {
  categoryCount: number;
  recipeCount: number;
  publishedRecipeCount: number;
  draftRecipeCount: number;
  archivedRecipeCount: number;
  byCategory: { categoryId: string; categoryName: string; recipeCount: number }[];
};

export default function AdminStatistics() {
  const [statistics, setStatistics] = useState<Statistics | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    apiFetch<Statistics>('/api/v1/dashboard/statistics')
      .then(setStatistics)
      .catch((reason: Error) => setError(reason.message || 'Không tải được thống kê.'));
  }, []);

  if (error) return <div className="empty-state"><strong>Không thể tải số liệu quản trị</strong><span>{error}</span></div>;
  if (!statistics) return <div className="loading-state">Đang tải thống kê hệ thống...</div>;

  const stats = [
    { label: 'Tổng công thức', value: statistics.recipeCount, icon: '🍲' },
    { label: 'Đã xuất bản', value: statistics.publishedRecipeCount, icon: '✦' },
    { label: 'Bản nháp', value: statistics.draftRecipeCount, icon: '◷' },
    { label: 'Danh mục hoạt động', value: statistics.categoryCount, icon: '▦' },
  ];
  const maxCount = Math.max(...statistics.byCategory.map((item) => item.recipeCount), 1);

  return (
    <>
      <div className="stats-grid">
        {stats.map((stat) => (
          <article className="stat-card" key={stat.label}>
            <span><i aria-hidden="true">{stat.icon}</i>{stat.label}</span>
            <strong>{stat.value.toLocaleString('vi-VN')}</strong>
          </article>
        ))}
      </div>
      <div className="admin-charts">
        <section className="flow-panel">
          <h2>Công thức theo danh mục</h2>
          <p>Phân bố nội dung theo các danh mục đang hoạt động.</p>
          {statistics.byCategory.length === 0 ? <p className="empty-inline">Chưa có dữ liệu danh mục.</p> : (
            <ul className="bar-list">
              {statistics.byCategory.map((category) => (
                <li className="bar-row" key={category.categoryId}>
                  <span>{category.categoryName}</span>
                  <span className="bar-track"><span className="bar-fill" style={{ width: `${Math.max(3, category.recipeCount / maxCount * 100)}%` }} /></span>
                  <strong>{category.recipeCount}</strong>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section className="flow-panel">
          <h2>Trạng thái nội dung</h2>
          <p>Tổng quan vòng đời công thức.</p>
          <ul className="status-breakdown">
            <li><span className="status status-published">Đã xuất bản</span><strong>{statistics.publishedRecipeCount}</strong></li>
            <li><span className="status status-draft">Bản nháp</span><strong>{statistics.draftRecipeCount}</strong></li>
            <li><span className="status status-archived">Đã lưu trữ</span><strong>{statistics.archivedRecipeCount}</strong></li>
          </ul>
          <a className="flow-panel-link" href="/admin/recipes">Đi tới quản lý công thức →</a>
        </section>
      </div>
    </>
  );
}
