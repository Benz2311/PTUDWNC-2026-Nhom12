'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiJson, saveAuth } from '@/lib/api';

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setIsSubmitting(true);

    try {
      const payload = await apiJson<{ accessToken: string; refreshToken: string; user: unknown }>('/api/v1/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      saveAuth(payload);
      router.push('/profile');
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể đăng nhập lúc này.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <>
      <div className="flow-kicker">Xác thực tài khoản</div>
      <h1 className="flow-title">Đăng nhập</h1>
      <p className="flow-description">Truy cập hồ sơ, công thức đã lưu và những trải nghiệm riêng của bạn.</p>
      <section className="auth-form-panel">
        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="field">Email<input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required autoComplete="email" /></label>
          <label className="field">Mật khẩu<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required autoComplete="current-password" /></label>
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="primary-button auth-submit" type="submit" disabled={isSubmitting}>{isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}</button>
        </form>
        <p className="auth-switch">Chưa có tài khoản? <a href="/register">Đăng ký ngay</a></p>
      </section>
    </>
  );
}
