export default function DiscoverySearchLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="flow-layout">
      <header className="flow-header">
        <a className="brand" href="/" aria-label="Culinary Blog home">
          <span className="brand-mark">♨</span>
          <span><span className="brand-name">Culinary Blog</span><span className="brand-subtitle">Khám phá & tìm kiếm</span></span>
        </a>
        <nav className="flow-nav" aria-label="Khám phá và tìm kiếm">
          <a href="/explore">Khám phá ẩm thực</a><a href="/search">Tìm kiếm</a><a href="/recipes">Công thức</a>
        </nav>
      </header>
      <main className="flow-main">{children}</main>
    </div>
  );
}
