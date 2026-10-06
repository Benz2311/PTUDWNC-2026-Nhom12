import SiteHeader from '@/components/SiteHeader';

export default function NotFound() {
  return (
    <div className="site-shell">
      <SiteHeader />
      <main className="flow-main">
        <section className="not-found">
          <span className="not-found-mark" aria-hidden="true">♨</span>
          <h1>404</h1>
          <h2>Trang không tồn tại</h2>
          <p>Có thể đường dẫn đã thay đổi hoặc công thức này không còn được chia sẻ. Hãy quay lại khám phá những món ăn khác nhé.</p>
          <a className="primary-button" href="/">Quay về trang chủ</a>
        </section>
      </main>
    </div>
  );
}
