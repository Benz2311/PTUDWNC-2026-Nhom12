import SiteHeader from '@/components/SiteHeader';

export default function AboutPage() {
  return (
    <div className="site-shell">
      <SiteHeader />
      <main className="flow-main about-page">
        <section className="about-banner">
          <div className="flow-kicker">Bếp nhà, câu chuyện riêng</div>
          <h1>Mỗi công thức đều bắt đầu từ một câu chuyện.</h1>
          <p>Culinary Blog là nơi cộng đồng yêu bếp khám phá món ngon, ghi lại bí quyết và chia sẻ những bữa ăn đáng nhớ.</p>
          <a className="primary-button" href="/recipes">Khám phá công thức</a>
        </section>
        <div className="flow-panel-grid">
          <section className="flow-panel"><h2>Khám phá</h2><p>Tìm cảm hứng cho bữa ăn tiếp theo từ những công thức được cộng đồng chia sẻ.</p><a className="flow-panel-link" href="/explore">Mở thư viện công thức →</a></section>
          <section className="flow-panel"><h2>Chia sẻ</h2><p>Ghi lại từng nguyên liệu, bước nấu và kinh nghiệm làm nên món ăn của bạn.</p><a className="flow-panel-link" href="/recipes/new">Tạo công thức mới →</a></section>
          <section className="flow-panel"><h2>Kết nối</h2><p>Cùng lan tỏa tình yêu ẩm thực và những câu chuyện trong căn bếp gia đình.</p><a className="flow-panel-link" href="/register">Tham gia cộng đồng →</a></section>
        </div>
      </main>
    </div>
  );
}
