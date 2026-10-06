import FeaturedRecipes from '@/components/discovery/FeaturedRecipes';
import SiteHeader from '@/components/SiteHeader';
import CategoryChips from '@/components/discovery/CategoryChips';

export default function HomePage() {
  return (
    <div className="site-shell">
      <SiteHeader />

      <main id="top">
        <section className="hero-wrap">
          <div className="hero">
            <div className="hero-content">
              <div className="eyebrow">Bếp nhà, câu chuyện riêng</div>
              <h1>Khám phá niềm vui <em>nấu ăn</em> mỗi ngày.</h1>
              <p className="hero-copy">
                Tìm cảm hứng từ hàng trăm công thức được chia sẻ bởi cộng đồng yêu bếp. Từ món quen thuộc đến những hương vị mới đang chờ bạn thử.
              </p>
              <form className="search-bar" role="search" action="/search">
                <span className="search-icon">⌕</span>
                <input aria-label="Tìm công thức" name="q" placeholder="Tìm công thức, món ăn..." />
                <button className="search-submit" type="submit">Tìm kiếm</button>
              </form>
              <div className="hero-note"><span>✦</span> Cảm hứng nấu ăn được chia sẻ mỗi ngày</div>
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
          <CategoryChips />

          <div className="section-heading">
            <div>
              <h2>Công thức nổi bật</h2>
              <p>Công thức mới được cộng đồng chia sẻ gần đây.</p>
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

          <section className="feature-ribbon" aria-label="Điểm nổi bật">
            <a href="/recipes"><span>✦</span><strong>Khám phá phong phú</strong><small>Ẩm thực đa dạng, dễ tìm kiếm</small></a>
            <a href="/recipes/new"><span>♧</span><strong>Hướng dẫn chi tiết</strong><small>Từng bước, nguyên liệu rõ ràng</small></a>
            <a href="/recipes/new"><span>↗</span><strong>Chia sẻ công thức</strong><small>Đóng góp cùng cộng đồng</small></a>
            <a href="/profile"><span>◎</span><strong>Tài khoản cá nhân</strong><small>Quản lý câu chuyện bếp nhà</small></a>
            <a href="/admin"><span>▥</span><strong>Quản trị minh bạch</strong><small>Theo dõi nội dung hệ thống</small></a>
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
