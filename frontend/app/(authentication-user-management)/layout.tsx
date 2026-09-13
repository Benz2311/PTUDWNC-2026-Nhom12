export default function AuthenticationUserManagementLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="flow-layout">
      <header className="flow-header">
        <a className="brand" href="/" aria-label="Culinary Blog home">
          <span className="brand-mark">♨</span>
          <span><span className="brand-name">Culinary Blog</span><span className="brand-subtitle">Xác thực & quản lý người dùng</span></span>
        </a>
        <nav className="flow-nav" aria-label="Xác thực và người dùng">
          <a href="/login">Đăng nhập</a><a href="/register">Đăng ký</a><a href="/profile">Hồ sơ cá nhân</a>
        </nav>
      </header>
      <main className="flow-main">{children}</main>
    </div>
  );
}
