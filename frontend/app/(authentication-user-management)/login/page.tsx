'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiJson, saveAuth } from '@/lib/api';

export default function LoginPage() {
  const router = useRouter();
  const [loginIdentifier, setLoginIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [canResendConfirmation, setCanResendConfirmation] = useState(false);
  const [isResending, setIsResending] = useState(false);
  const [resendMessage, setResendMessage] = useState('');

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setCanResendConfirmation(false);
    setResendMessage('');
    setIsSubmitting(true);

    try {
      const payload = await apiJson<{ accessToken: string; refreshToken: string; user: unknown }>('/api/v1/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userNameOrEmail: loginIdentifier, password })
      }, false);
      saveAuth(payload);
      router.replace('/');
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể đăng nhập lúc này.');
      const status = submitError instanceof Error && 'status' in submitError
        ? submitError.status
        : undefined;
      setCanResendConfirmation(status === 401 && loginIdentifier.includes('@'));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function resendConfirmationEmail() {
    setIsResending(true);
    setResendMessage('');
    try {
      await apiJson<void>('/api/v1/auth/email/resend', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: loginIdentifier }),
      }, false);
      setResendMessage('Nếu tài khoản tồn tại và chưa xác nhận, email xác nhận sẽ được gửi lại.');
    } catch (resendError) {
      setResendMessage(resendError instanceof Error ? resendError.message : 'Không thể gửi lại email xác nhận.');
    } finally {
      setIsResending(false);
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
          <label className="field">Email hoặc tên người dùng<input type="text" value={loginIdentifier} onChange={(event) => setLoginIdentifier(event.target.value)} required autoComplete="username" /></label>
          <label className="field">Mật khẩu<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required autoComplete="current-password" /></label>
          <a className="auth-switch" href="/forgot-password">Quên mật khẩu?</a>
          {error && <p className="form-error" role="alert">{error}</p>}
          {canResendConfirmation && (
            <div>
              <button className="outline-button" type="button" onClick={resendConfirmationEmail} disabled={isResending}>
                {isResending ? 'Đang gửi...' : 'Gửi lại email xác nhận'}
              </button>
              {resendMessage && <p role="status">{resendMessage}</p>}
            </div>
          )}
          <button className="primary-button auth-submit" type="submit" disabled={isSubmitting}>{isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}</button>
        </form>
        <p className="auth-switch">Chưa có tài khoản? <a href="/register">Đăng ký ngay</a></p>
      </section>
      </div>
    </div>
  );
}
