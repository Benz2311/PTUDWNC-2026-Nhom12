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
      }, false);
      saveAuth(payload);
      router.replace('/');
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể đăng nhập lúc này.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="auth-stage">
      <section className="auth-story">
        <div className="flow-kicker">Chào mừng trở lại</div>
        <h2 className="flow-title">Mỗi ngày là một món ngon mới.</h2>
        <p>Đăng nhập để tiếp tục lưu lại công thức yêu thích, chia sẻ câu chuyện căn bếp và khám phá món ăn từ cộng đồng.</p>
        <ul className="auth-benefits"><li>Theo dõi công thức của riêng bạn</li><li>Quản lý nguyên liệu và các bước nấu</li><li>Kết nối cùng những người yêu bếp</li></ul>
      </section>
      <div className="auth-stage-form">
        <div className="flow-kicker">Tài khoản Culinary Blog</div>
        <h1 className="flow-title">Đăng nhập</h1>
        <p className="flow-description">Rất vui được gặp lại bạn. Mời bạn tiếp tục hành trình vào bếp.</p>
        <section className="auth-form-panel">
        <h2>Chào mừng trở lại!</h2><p>Nhập thông tin đăng nhập để tiếp tục.</p>
        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="field">Email<input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required autoComplete="email" /></label>
          <label className="field">Mật khẩu<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required autoComplete="current-password" /></label>
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="primary-button auth-submit" type="submit" disabled={isSubmitting}>{isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}</button>
        </form>
        <p className="auth-switch">Chưa có tài khoản? <a href="/register">Đăng ký ngay</a></p>
      </section>
      </div>
    </div>
  );
}
