'use client';

import { FormEvent, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch, apiJson, clearAuth } from '@/lib/api';
import type { ApiError } from '@/lib/api';

type User = { fullName: string; email: string; userName?: string; avatarUrl?: string | null; roles?: string[]; emailConfirmed?: boolean };

export default function ProfilePage() {
  const router = useRouter();
  const [user, setUser] = useState<User | null>(null);
  const [loadError, setLoadError] = useState('');
  const [fullName, setFullName] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [message, setMessage] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!localStorage.getItem('culinary_access_token')) {
      router.replace('/login');
      return;
    }

    apiJson<User>('/api/v1/auth/me')
      .then((profile) => {
        setUser(profile);
        setFullName(profile.fullName);
        setAvatarUrl(profile.avatarUrl ?? '');
      })
      .catch((error: ApiError) => {
        if (error.status === 401) {
          clearAuth();
          router.replace('/login');
          return;
        }

        setLoadError('Không thể tải hồ sơ lúc này. Phiên đăng nhập vẫn được giữ; vui lòng thử lại.');
      });
  }, [router]);

  async function saveProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setMessage('');
    try {
      const updated = await apiFetch<User>('/api/v1/auth/me', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ fullName, avatarUrl: avatarUrl || null }),
      });
      setUser(updated);
      setMessage('Thông tin hồ sơ đã được cập nhật.');
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : 'Không thể cập nhật hồ sơ.');
    } finally {
      setSaving(false);
    }
  }

  async function handleLogout() {
    try {
      await apiJson<User>('/api/v1/auth/me');
      await apiFetch('/api/v1/auth/logout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: localStorage.getItem('culinary_refresh_token') }),
      });
    } catch {
      // End the local session even if the API is unavailable.
    } finally {
      clearAuth();
      router.replace('/');
    }
  }

  if (loadError) {
    return <p className="form-error" role="alert">{loadError} <button className="text-button" type="button" onClick={() => window.location.reload()}>Thử lại</button></p>;
  }

  if (!user) {
    return <p className="loading-state">Đang tải hồ sơ...</p>;
  }

  return (
    <>
      <div className="flow-kicker">Hồ sơ cá nhân</div>
      <h1 className="flow-title">Bếp của {user.fullName}</h1>
      <p className="flow-description">Quản lý thông tin tài khoản và những công thức bạn đã chia sẻ.</p>
      <div className="profile-workspace">
        <aside className="recipe-filter-panel profile-menu">
          <div className="profile-avatar" role={user.avatarUrl ? 'img' : undefined} aria-label={user.avatarUrl ? `Ảnh đại diện của ${user.fullName}` : undefined} style={user.avatarUrl ? { backgroundImage: `url("${user.avatarUrl}")` } : undefined}>{user.avatarUrl ? null : user.fullName.slice(0, 1).toUpperCase()}</div>
          <strong>{user.fullName}</strong><span>{user.email}</span>
          <a href="/profile" className="is-current">◉ Hồ sơ cá nhân</a>
          <a href="/recipes">▤ Công thức của tôi</a>
          <a href="/recipes/new">＋ Chia sẻ công thức</a>
          {user.roles?.includes('Admin') && <a href="/admin">⚙ Quản trị hệ thống</a>}
          <button className="outline-button" type="button" onClick={handleLogout}>Đăng xuất</button>
        </aside>
        <section className="form-section profile-form-panel">
          <div className="form-section-heading"><span className="step-number">01</span><div><h2>Thông tin cá nhân</h2><p>Cập nhật tên hiển thị và ảnh đại diện.</p></div></div>
          <form className="auth-form" onSubmit={saveProfile}>
            <label className="field"><span>Họ và tên</span><input required minLength={1} maxLength={100} value={fullName} onChange={(event) => setFullName(event.target.value)} /></label>
            <label className="field"><span>Email đăng nhập</span><input value={user.email} disabled /></label>
            <label className="field"><span>Đường dẫn ảnh đại diện</span><input type="url" value={avatarUrl} onChange={(event) => setAvatarUrl(event.target.value)} placeholder="https://..." /></label>
            <label className="field"><span>Trạng thái email</span><input value={user.emailConfirmed ? 'Đã xác nhận' : 'Chưa xác nhận'} disabled /></label>
            {message && <p className={message.startsWith('Thông tin') ? 'form-success' : 'form-error'} role="status">{message}</p>}
            <div className="form-actions"><button className="primary-button" type="submit" disabled={saving}>{saving ? 'Đang lưu...' : 'Lưu thay đổi'}</button></div>
          </form>
        </section>
      </div>
    </>
  );
}
