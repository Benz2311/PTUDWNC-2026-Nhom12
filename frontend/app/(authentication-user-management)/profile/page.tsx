'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch, apiJson, clearAuth } from '@/lib/api';

type User = { fullName: string; email: string; userName?: string };

export default function ProfilePage() {
  const router = useRouter();
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    if (!localStorage.getItem('culinary_access_token')) {
      router.replace('/login');
      return;
    }

    apiJson<User>('/api/v1/auth/me')
      .then(setUser)
      .catch(() => {
        clearAuth();
        router.replace('/login');
      });
  }, [router]);

  async function handleLogout() {
    await apiFetch('/api/v1/auth/logout', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: localStorage.getItem('culinary_refresh_token') }),
    }, false).catch(() => undefined);
    clearAuth();
    router.replace('/login');
  }

  if (!user) {
    return <p className="loading-state">Đang tải hồ sơ...</p>;
  }

  return (
    <>
      <div className="flow-kicker">Quản lý người dùng</div>
      <h1 className="flow-title">Xin chào, {user.fullName}</h1>
      <p className="flow-description">{user.email}</p>
      <div className="profile-actions">
        <button className="outline-button" type="button" onClick={handleLogout}>Đăng xuất</button>
      </div>
      <div className="flow-panel-grid">
        <section className="flow-panel"><h2>Thông tin cá nhân</h2><p>Họ tên, email, ảnh đại diện và giới thiệu ngắn.</p></section>
        <section className="flow-panel"><h2>Công thức của tôi</h2><p>Theo dõi những công thức bạn đã viết và xuất bản.</p></section>
        <section className="flow-panel"><h2>Đã lưu</h2><p>Tập hợp các món ăn yêu thích để nấu lại sau.</p></section>
      </div>
    </>
  );
}
