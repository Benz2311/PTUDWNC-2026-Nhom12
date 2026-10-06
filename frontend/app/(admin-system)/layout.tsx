import SiteHeader from '@/components/SiteHeader';

export default function AdminSystemLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="flow-layout admin-layout">
      <SiteHeader admin />
      <nav className="admin-subnav" aria-label="Điều hướng quản trị">
        <a href="/admin">Tổng quan</a><a href="/admin/recipes">Công thức</a><a href="/admin/users">Người dùng</a><a href="/admin/reviews">Đánh giá</a><a href="/admin/reports">Thống kê & báo cáo</a>
      </nav>
      <main className="flow-main">{children}</main>
    </div>
  );
}
