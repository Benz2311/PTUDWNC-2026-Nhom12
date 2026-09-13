export default function AdminSystemLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="flow-layout admin-layout">
      <header className="flow-header">
        <a className="brand" href="/" aria-label="Culinary Blog home">
          <span className="brand-mark">♨</span>
          <span><span className="brand-name">Culinary Blog</span><span className="brand-subtitle">Quản trị viên · Hệ thống</span></span>
        </a>
        <nav className="flow-nav" aria-label="Quản trị hệ thống">
          <a href="/admin">Dashboard</a><a href="/admin/users">Người dùng</a><a href="/admin/recipes">Công thức</a><a href="/admin/reports">Báo cáo</a>
        </nav>
      </header>
      <main className="flow-main">{children}</main>
    </div>
  );
}
