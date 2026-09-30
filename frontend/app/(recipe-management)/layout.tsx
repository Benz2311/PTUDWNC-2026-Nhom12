export default function RecipeManagementLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="flow-layout">
      <header className="flow-header">
        <a className="brand" href="/" aria-label="Culinary Blog home">
          <span className="brand-mark">♨</span>
          <span><span className="brand-name">Culinary Blog</span><span className="brand-subtitle">Quản lý công thức</span></span>
        </a>
        <nav className="flow-nav" aria-label="Quản lý công thức">
          <a href="/">Trang chủ</a><a href="/recipes">Danh sách công thức</a><a href="/recipes/new">Tạo công thức</a><a href="/profile">Công thức của tôi</a>
        </nav>
      </header>
      <main className="flow-main">{children}</main>
    </div>
  );
}
