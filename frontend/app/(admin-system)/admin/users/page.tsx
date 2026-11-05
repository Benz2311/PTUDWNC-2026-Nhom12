import AdminUserList from '@/components/admin/AdminUserList';

export default function AdminUsersPage() {
  return (
    <>
      <div className="flow-kicker">Quản trị viên · Hệ thống</div>
      <h1 className="flow-title">Quản lý người dùng</h1>
      <p className="flow-description">Theo dõi tài khoản, vai trò và trạng thái hoạt động của cộng đồng.</p>
      <AdminUserList />
    </>
  );
}
