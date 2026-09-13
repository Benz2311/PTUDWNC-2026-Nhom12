const categories = [
  ["✨", "Tất cả"],
  ["🍜", "Món Việt"],
  ["🍝", "Món Âu"],
  ["🥗", "Ăn lành mạnh"],
  ["🍰", "Tráng miệng"],
  ["☕", "Đồ uống"],
];

const recipes = [
  {
    title: "Phở bò truyền thống",
    description: "Nước dùng trong, thơm vị quế hồi và những lát bò mềm.",
    tag: "Món Việt",
    time: "45 phút",
    rating: "4.9",
    image: "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=85",
  },
  {
    title: "Gà nướng mật ong",
    description: "Lớp da vàng óng, vị ngọt dịu và công thức thật dễ làm.",
    tag: "Được yêu thích",
    time: "60 phút",
    rating: "4.8",
    image: "https://images.unsplash.com/photo-1532550907401-a500c9a57435?auto=format&fit=crop&w=800&q=85",
  },
  {
    title: "Bánh mì bò bít tết",
    description: "Bữa sáng giòn thơm với nhân bò đậm đà kiểu nhà làm.",
    tag: "Bữa sáng",
    time: "25 phút",
    rating: "4.7",
    image: "https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?auto=format&fit=crop&w=800&q=85",
  },
  {
    title: "Pasta sốt kem",
    description: "Món pasta mềm mượt, hoàn thành trong một chiếc chảo.",
    tag: "Món Âu",
    time: "30 phút",
    rating: "4.9",
    image: "https://images.unsplash.com/photo-1473093295043-cdd812d0e601?auto=format&fit=crop&w=800&q=85",
  },
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
          <a href="#recipes">Khám phá ẩm thực</a>
          <a href="#recipes">Học nấu ăn</a>
          <a href="#community">Kết nối cộng đồng</a>
        </nav>

        <div className="auth-actions">
          <button className="text-button" type="button">Đăng nhập</button>
          <button className="primary-button" type="button">Đăng ký miễn phí</button>
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
                <button type="button">Tìm kiếm</button>
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
              <button className="category" key={label} type="button">
                <span className="category-icon">{icon}</span>{label}
              </button>
            ))}
          </div>

          <div className="section-heading">
            <div>
              <h2>Công thức nổi bật</h2>
              <p>Những món ăn được cộng đồng yêu thích tuần này.</p>
            </div>
            <a className="section-link" href="#recipes">Xem tất cả →</a>
          </div>
          <div className="recipe-grid">
            {recipes.map((recipe) => (
              <article className="recipe-card" key={recipe.title}>
                <div className="recipe-image" style={{ backgroundImage: `url(${recipe.image})` }}>
                  <span className="recipe-tag">{recipe.tag}</span>
                </div>
                <div className="recipe-body">
                  <h3>{recipe.title}</h3>
                  <p>{recipe.description}</p>
                  <div className="recipe-meta">
                    <span>◷ {recipe.time}</span>
                    <span className="rating">★ {recipe.rating}</span>
                  </div>
                </div>
              </article>
            ))}
          </div>

          <section className="community-band" id="community">
            <div>
              <h2>Bữa cơm ngon hơn khi được sẻ chia.</h2>
              <p>Lưu lại công thức yêu thích, viết câu chuyện của bạn và cùng kết nối với những người yêu bếp.</p>
            </div>
            <div className="community-actions">
              <button className="outline-button" type="button">Khám phá thêm</button>
              <button className="primary-button" type="button">Tham gia ngay</button>
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
