import AdminDataPlaceholder from '@/components/admin/AdminDataPlaceholder';

export default function AdminUsersPage() {
  return (
    <>
      <div className="flow-kicker">Quản trị viên · Hệ thống</div>
      <h1 className="flow-title">Quản lý người dùng</h1>
      <p className="flow-description">Theo dõi tài khoản, vai trò và trạng thái hoạt động của cộng đồng.</p>
      <AdminDataPlaceholder title="Danh sách thành viên" description="Quản lý tài khoản và quyền truy cập." columns={['Thành viên', 'Email', 'Vai trò', 'Công thức', 'Trạng thái', 'Thao tác']} />
    </>
  );
}
