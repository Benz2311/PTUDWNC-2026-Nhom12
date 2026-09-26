import FeaturedRecipes from '@/components/discovery/FeaturedRecipes';

const categories = [
  ["✨", "Tất cả"],
  ["🍜", "Món Việt"],
  ["🍝", "Món Âu"],
  ["🥗", "Ăn lành mạnh"],
  ["🍰", "Tráng miệng"],
  ["☕", "Đồ uống"],
];

export default function HomePage() {
  return (
    <div className="site-shell">
      <header className="topbar">
        <a className="brand" href="#top" aria-label="Culinary Blog home">
          <span className="brand-mark">♨</span>
          <span>
            <span className="brand-name">Culinary Blog</span>
            <span className="brand-subtitle">Hành trình trải nghiệm ứng dụng</span>
          </span>
        </a>

        <nav className="main-nav" aria-label="Điều hướng chính">
          <a href="/recipes">Khám phá ẩm thực</a>
          <a href="/recipes/new">Học nấu ăn</a>
          <a href="#community">Kết nối cộng đồng</a>
        </nav>

        <div className="auth-actions">
          <a className="text-button" href="/login">Đăng nhập</a>
          <a className="primary-button" href="/register">Đăng ký miễn phí</a>
        </div>
      </header>

      <main id="top">
        <section className="hero-wrap">
          <div className="hero">
            <div className="hero-content">
              <div className="eyebrow">Bếp nhà, câu chuyện riêng</div>
              <h1>Khám phá niềm vui <em>nấu ăn</em> mỗi ngày.</h1>
              <p className="hero-copy">
                Tìm cảm hứng từ hàng trăm công thức được chia sẻ bởi cộng đồng yêu bếp. Từ món quen thuộc đến những hương vị mới đang chờ bạn thử.
              </p>
              <div className="search-bar" role="search">
                <span className="search-icon">⌕</span>
                <input aria-label="Tìm công thức" placeholder="Tìm công thức, món ăn..." />
                <a className="search-submit" href="/search">Tìm kiếm</a>
              </div>
              <div className="hero-note"><span>✦</span> Hơn 1.200 công thức đã được chia sẻ</div>
            </div>
          </div>
        </section>

        <section className="content" id="recipes">
          <div className="section-heading">
            <div>
              <h2>Khám phá theo khẩu vị</h2>
              <p>Chọn một chủ đề và bắt đầu hành trình vào bếp.</p>
            </div>
          </div>
          <div className="category-row" aria-label="Danh mục món ăn">
            {categories.map(([icon, label]) => (
              <a className="category" href="/recipes" key={label}>
                <span className="category-icon">{icon}</span>{label}
              </a>
            ))}
          </div>

          <div className="section-heading">
            <div>
              <h2>Công thức nổi bật</h2>
              <p>Những món ăn được cộng đồng yêu thích tuần này.</p>
            </div>
            <a className="section-link" href="/recipes">Xem tất cả →</a>
          </div>
          <FeaturedRecipes />

          <section className="community-band" id="community">
            <div>
              <h2>Bữa cơm ngon hơn khi được sẻ chia.</h2>
              <p>Lưu lại công thức yêu thích, viết câu chuyện của bạn và cùng kết nối với những người yêu bếp.</p>
            </div>
            <div className="community-actions">
              <a className="outline-button" href="/recipes">Khám phá thêm</a>
              <a className="primary-button" href="/register">Tham gia ngay</a>
            </div>
          </section>

          <footer className="footer">
            <span>© 2026 Culinary Blog</span>
            <span>Nấu ăn là ngôn ngữ của yêu thương.</span>
          </footer>
        </section>
      </main>
    </div>
  );
}
