'use client';

import { useEffect, useState } from 'react';
import { apiJson } from '@/lib/api';

type ConfirmationStatus = 'loading' | 'confirmed' | 'error';

type ConfirmEmailPageProps = {
  searchParams: { token?: string | string[] };
};

export default function ConfirmEmailPage({ searchParams }: ConfirmEmailPageProps) {
  const token = Array.isArray(searchParams.token) ? searchParams.token[0] : searchParams.token;
  const [status, setStatus] = useState<ConfirmationStatus>(token ? 'loading' : 'error');
  const [message, setMessage] = useState(token ? 'Đang xác nhận email...' : 'Liên kết xác nhận không hợp lệ.');

  useEffect(() => {
    let active = true;

    if (!token) {
      return () => {
        active = false;
      };
    }

    apiJson<void>(
      '/api/v1/auth/email/confirm',
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token }),
      },
      false,
    ).then(() => {
      if (active) {
        setStatus('confirmed');
        setMessage('Email đã được xác nhận. Bạn có thể đăng nhập.');
      }
    }).catch((error: unknown) => {
      if (active) {
        setStatus('error');
        setMessage(error instanceof Error ? error.message : 'Không thể xác nhận email.');
      }
    });

    return () => {
      active = false;
    };
  }, [token]);

  return (
    <div className="auth-stage">
      <section className="auth-story">
        <div className="flow-kicker">Tài khoản Culinary Blog</div>
        <h1 className="flow-title">Xác nhận địa chỉ email</h1>
        <p>Email đã xác nhận mới có thể xuất bản công thức lên cộng đồng.</p>
      </section>
      <div className="auth-stage-form">
        <section className="auth-form-panel" aria-live="polite">
          <h2>{status === 'confirmed' ? 'Email đã sẵn sàng' : status === 'loading' ? 'Đang xử lý' : 'Không thể xác nhận'}</h2>
          <p role={status === 'error' ? 'alert' : 'status'}>{message}</p>
          {status === 'confirmed' && <a className="primary-button auth-submit" href="/login">Đăng nhập</a>}
          {status === 'error' && <a className="outline-button" href="/register">Đăng ký lại</a>}
        </section>
      </div>
    </div>
  );
}