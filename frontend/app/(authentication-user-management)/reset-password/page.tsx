'use client';

import { FormEvent, useState } from 'react';
import { apiJson } from '@/lib/api';

type ResetPasswordPageProps = {
  searchParams: { token?: string | string[] };
};

export default function ResetPasswordPage({ searchParams }: ResetPasswordPageProps) {
  const token = Array.isArray(searchParams.token) ? searchParams.token[0] : searchParams.token;
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isReset, setIsReset] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');

    if (!token) {
      setError('Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.');
      return;
    }
    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.');
      return;
    }

    setIsSubmitting(true);
    try {
      await apiJson<void>('/api/v1/auth/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token, newPassword: password }),
      }, false);
      setIsReset(true);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Không thể đặt lại mật khẩu.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="auth-stage">
      <section className="auth-story">
        <div className="flow-kicker">Bảo mật tài khoản</div>
        <h1 className="flow-title">Tạo mật khẩu mới</h1>
        <p>Chọn mật khẩu có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.</p>
      </section>
      <div className="auth-stage-form">
        <section className="auth-form-panel">
          {isReset ? (
            <>
              <h2>Đổi mật khẩu thành công</h2>
              <p role="status">Bạn có thể đăng nhập bằng mật khẩu mới.</p>
              <a className="primary-button auth-submit" href="/login">Đăng nhập</a>
            </>
          ) : (
            <>
              <h2>Đặt lại mật khẩu</h2>
              <form className="auth-form" onSubmit={handleSubmit}>
                <label className="field">
                  Mật khẩu mới
                  <input
                    type="password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    required
                    minLength={8}
                    autoComplete="new-password"
                  />
                </label>
                <label className="field">
                  Xác nhận mật khẩu mới
                  <input
                    type="password"
                    value={confirmPassword}
                    onChange={(event) => setConfirmPassword(event.target.value)}
                    required
                    minLength={8}
                    autoComplete="new-password"
                  />
                </label>
                {error && <p className="form-error" role="alert">{error}</p>}
                <button className="primary-button auth-submit" type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Đang cập nhật...' : 'Đặt lại mật khẩu'}
                </button>
              </form>
              <p className="auth-switch"><a href="/forgot-password">Yêu cầu liên kết mới</a></p>
            </>
          )}
        </section>
      </div>
    </div>
  );
}
