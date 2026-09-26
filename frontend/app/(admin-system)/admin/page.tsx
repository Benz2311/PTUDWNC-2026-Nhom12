import AdminStatistics from '@/components/admin/AdminStatistics';

export default function AdminDashboardPage() {
  return <><div className="flow-kicker">Quản trị viên · Hệ thống</div><h1 className="flow-title">Tổng quan hệ thống</h1><p className="flow-description">Theo dõi nội dung công thức và phân bổ danh mục từ dữ liệu thực tế.</p><AdminStatistics /></>;
}
