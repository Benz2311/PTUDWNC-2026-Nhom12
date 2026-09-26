'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiJson, saveAuth } from '@/lib/api';

export default function RegisterPage() {
  const router = useRouter();
  const [form, setForm] = useState({ fullName: '', email: '', userName: '', password: '', confirmPassword: '' });
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  function updateField(field: keyof typeof form, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    if (form.password !== form.confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.');
      return;
    }

    setIsSubmitting(true);
    try {
      const payload = await apiJson<{ accessToken: string; refreshToken: string; user: unknown }>('/api/v1/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ fullName: form.fullName, email: form.email, userName: form.userName || null, password: form.password })
      });
      saveAuth(payload);
      router.push('/profile');
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể đăng ký lúc này.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <>
      <div className="flow-kicker">Xác thực tài khoản</div>
      <h1 className="flow-title">Tạo tài khoản</h1>
      <p className="flow-description">Tham gia cộng đồng yêu bếp, lưu lại cảm hứng và chia sẻ công thức của riêng bạn.</p>
      <section className="auth-form-panel">
        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="field">Họ và tên<input value={form.fullName} onChange={(event) => updateField('fullName', event.target.value)} required autoComplete="name" /></label>
          <label className="field">Email<input type="email" value={form.email} onChange={(event) => updateField('email', event.target.value)} required autoComplete="email" /></label>
          <label className="field">Tên người dùng<input value={form.userName} onChange={(event) => updateField('userName', event.target.value)} autoComplete="username" /></label>
          <div className="auth-form-grid">
            <label className="field">Mật khẩu<input type="password" value={form.password} onChange={(event) => updateField('password', event.target.value)} required minLength={6} autoComplete="new-password" /></label>
            <label className="field">Xác nhận mật khẩu<input type="password" value={form.confirmPassword} onChange={(event) => updateField('confirmPassword', event.target.value)} required minLength={6} autoComplete="new-password" /></label>
          </div>
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="primary-button auth-submit" type="submit" disabled={isSubmitting}>{isSubmitting ? 'Đang tạo tài khoản...' : 'Tạo tài khoản'}</button>
        </form>
        <p className="auth-switch">Đã có tài khoản? <a href="/login">Đăng nhập</a></p>
      </section>
    </>
  );
}
