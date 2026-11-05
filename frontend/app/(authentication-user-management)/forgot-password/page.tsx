'use client';

import { FormEvent, useState } from 'react';
import { apiJson } from '@/lib/api';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setIsSubmitting(true);

    try {
      await apiJson<void>('/api/v1/auth/forgot-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email }),
      }, false);
      setIsSubmitted(true);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể gửi yêu cầu lúc này.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="auth-stage">
      <section className="auth-story">
        <div className="flow-kicker">Khôi phục tài khoản</div>
        <h1 className="flow-title">Lấy lại quyền truy cập</h1>
        <p>Nhập email đã đăng ký. Nếu tài khoản tồn tại, chúng tôi sẽ gửi liên kết đặt lại mật khẩu.</p>
      </section>
      <div className="auth-stage-form">
        <section className="auth-form-panel">
          {isSubmitted ? (
            <>
              <h2>Kiểm tra email của bạn</h2>
              <p role="status">Nếu email này thuộc một tài khoản đang hoạt động, hướng dẫn đặt lại mật khẩu sẽ được gửi đến hộp thư của bạn.</p>
              <a className="primary-button auth-submit" href="/login">Quay lại đăng nhập</a>
            </>
          ) : (
            <>
              <h2>Quên mật khẩu?</h2>
              <p>Liên kết đặt lại có hiệu lực trong một giờ.</p>
              <form className="auth-form" onSubmit={handleSubmit}>
                <label className="field">
                  Email
                  <input
                    type="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    required
                    autoComplete="email"
                  />
                </label>
                {error && <p className="form-error" role="alert">{error}</p>}
                <button className="primary-button auth-submit" type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Đang gửi...' : 'Gửi liên kết đặt lại'}
                </button>
              </form>
              <p className="auth-switch"><a href="/login">Quay lại đăng nhập</a></p>
            </>
          )}
        </section>
      </div>
    </div>
  );
}
